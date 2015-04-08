#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BehaviorTree
{
	public class BehaviorTreeEditor : EditorWindow
	{
		public static Type[] nodeTypes;
		public static String[] nodeTypeNames;

		private BehaviorTree _behaviorTree;
		private Dictionary<int, EditorData> _editorData = new Dictionary<int, EditorData>();

		// Default settings.
		private const float _nodeWidth = 200.0f;
		private const float _nodeHeight = 100.0f;

		// Decorator Settings
		private const float _decoratorHeight = 20;

		// Compositor Settings
		private const float _compositorHeight = 50;

		// Leaf settings.
		private const float _leafHeight = 200.0f;

		private Texture _plusIcon;
		private Texture _leftIcon;
		private Texture _rightIcon;

		/**
		 * @brief Add a menu item in the editor for creating new behavior tree assets.
		 *
		 * @todo
		 *     Create new asset in currently selected folder of the project view,
		 *     rather than always placing them in the root Assets folder.
		 *
		 * @todol
		 *     Check if asset already exists, because by default the AssetDatabase
		 *     will overwrite an existing asset of the same name.
		 */
		[MenuItem( "Assets/Create/Behavior Tree" )]
		public static void CreateBehaviorTreeAsset()
		{
			// Create behavior tree asset.
			BehaviorTree behaviorTreeAsset = ScriptableObject.CreateInstance<BehaviorTree>();
			AssetDatabase.CreateAsset( behaviorTreeAsset, "Assets/NewBehaviorTree.asset" );

			// Add root node to behavior tree.
			TreeNode newNode = (TreeNode)ScriptableObject.CreateInstance( typeof( NullNode ) );
			newNode.id = newNode.GetInstanceID(); // generate a unique ID for the node.
			behaviorTreeAsset._editorData.Add( new EditorData( newNode, null ) );
			behaviorTreeAsset.root = newNode;
			AssetDatabase.AddObjectToAsset( newNode, behaviorTreeAsset );

			// Set asset as the active object.
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = behaviorTreeAsset;
		}

		[MenuItem( "Window/Behavior Tree Editor" )]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow<BehaviorTreeEditor>();
		}

		void BuildEditorData()
		{
			_editorData.Clear();

			foreach ( EditorData editorData in _behaviorTree._editorData )
			{
				_editorData.Add( editorData.id, editorData );
			}
		}

		void OnGUI()
		{
			try
			{
				// always create a box for selecting the behavior tree
				_behaviorTree =
					(BehaviorTree)EditorGUILayout.ObjectField(
						"Behavior Tree",
						_behaviorTree,
						typeof( BehaviorTree ),
						false );

				// only draw editor if a behavior tree is selected
				if ( _behaviorTree )
				{
					// Perform initial setup.
					CreateTypeLists();
					LoadResources();
					BuildEditorData();

					CreateGUI();
				}
			}
			catch ( Exception e )
			{
				Debug.LogError( e );
			}
		}

		void CreateGUI()
		{
			BeginWindows();
			DrawNode( _behaviorTree.root );
			EndWindows();
		}

		#region Draw Nodes
		void DrawNode( TreeNode node )
		{
			if ( !node )
			{
				Debug.LogError( "Node is null!" );
				return;
			}

			// Handle children.
			if ( node is Decorator )
			{
				DrawDecorator( (Decorator)node );
			}
			else if ( node is Compositor )
			{
				DrawCompositor( (Compositor)node );
			}
			else
			{
				DrawLeaf( (LeafNode)node );
			}
		}

		void DrawDecorator( Decorator decorator )
		{
			EditorData nodeData = _editorData[decorator.id];
			CreateNodeWindow( nodeData, _nodeWidth, _decoratorHeight );

			// ensure that the node has a child
			if ( !decorator._child )
			{
				decorator._child = CreateNodeWithParent( typeof( NullNode ), decorator );
			}

			EditorData childData = _editorData[decorator._child.id];

			if ( DrawPlusButton( nodeData, childData ) )
			{
				Decorator newNode = (Decorator)CreateNodeWithParent( typeof( Succeed ), decorator );
				newNode._child = decorator._child;
				decorator._child = newNode;
			}

			// handle drawing the child
			DrawNodeCurve( nodeData.rect, childData.rect );
			DrawNode( decorator._child );
		}

		void DrawCompositor( Compositor compositor )
		{
			EditorData nodeData = _editorData[compositor.id];
			CreateNodeWindow( nodeData, _nodeWidth, _compositorHeight );

			for ( int index = 0; index < compositor._children.Count; ++index )
			{
				TreeNode child = compositor._children[index];
				EditorData childData = _editorData[child.id];

				if ( DrawPlusButton( nodeData, childData ) )
				{
					Decorator newChild = (Decorator)CreateNodeWithParent( typeof( Succeed ), compositor );
					newChild._child = child;
					compositor._children[index] = newChild;

					child = newChild;
				}

				if ( index > 0 &&
				     DrawLeftButton( childData ) )
				{
					SwapCompositorChildren( compositor, index, index - 1 );
				}

				if ( index < compositor._children.Count - 1 &&
				     DrawRightButton( childData ) )
				{
					SwapCompositorChildren( compositor, index, index + 1 );
				}

				DrawNodeCurve( nodeData.rect, childData.rect, index, compositor._children.Count );
				DrawNode( child );
			}
		}

		void DrawLeaf( LeafNode leaf )
		{
			EditorData nodeData = _editorData[leaf.id];
			CreateNodeWindow( nodeData, _nodeWidth, _leafHeight );
		}
		#endregion

		#region Startup Helpers
		void CreateTypeLists()
		{
			// get list of possible node types
			// i.e., all types that extend TreeNode and are not abstract
			nodeTypes = typeof( TreeNode ).Assembly.GetTypes()
					.Where( type => type.IsSubclassOf( typeof( TreeNode ) ) && !type.IsAbstract ).ToArray<Type>();
			nodeTypeNames = Array.ConvertAll<Type, String>( nodeTypes,
				new Converter<Type, String> (
					delegate( Type type )
					{
						if ( type.IsSubclassOf( typeof( Decorator ) ) )
						{
							return "Decorators/" + type.Name;
						}
						else if ( type.IsSubclassOf( typeof( Compositor ) ) )
						{
							return "Compositors/" + type.Name;
						}
						else
						{
							return "Leaves/" + type.Name;
						}
					} ) );
		}

		void LoadResources()
		{
			_plusIcon = (Texture)Resources.Load( "plus" );
			_leftIcon = (Texture)Resources.Load( "arrow_left" );
			_rightIcon = (Texture)Resources.Load( "arrow_right" );
		}
		#endregion

		#region GUI Helpers
		Type CreateNodeTypeDropdown( TreeNode node )
		{
			int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : Array.IndexOf<Type>( nodeTypes, typeof( NullNode ) ) );
			Rect rect = new Rect( 0, 0, _nodeWidth - 40.0f, 20.0f );
			selectedType = EditorGUI.Popup( rect, selectedType, nodeTypeNames );
			return nodeTypes[selectedType];
		}

		bool DrawPlusButton( EditorData parent, EditorData child )
		{
			float midX = ( ( parent.rect.x + _nodeWidth * 0.5f ) + ( child.rect.x + _nodeWidth * 0.5f ) ) * 0.5f;
			float midY = ( ( parent.rect.y + parent.rect.height ) + child.rect.y ) * 0.5f;

			Rect position = new Rect( midX + 2, midY - 8, 16, 16 );
			GUIStyle style = new GUIStyle();
			style.margin = new RectOffset( 0, 0, 0, 0 );
			style.border = new RectOffset( 0, 0, 0, 0 );
			return GUI.Button( position, _plusIcon, style );
		}

		bool DrawLeftButton( EditorData child )
		{
			float left = child.rect.x - 18;
			float top = child.rect.y;
			Rect position = new Rect( left, top, 16, 16 );
			GUIStyle style = new GUIStyle();
			style.margin = new RectOffset( 0, 0, 0, 0 );
			style.border = new RectOffset( 0, 0, 0, 0 );
			return GUI.Button( position, _leftIcon, style );
		}

		bool DrawRightButton( EditorData child )
		{
			float left = child.rect.x - 18;
			float top = child.rect.y + ( child.parentIndex == 0 ? 0 : 18 );
			Rect position = new Rect( left, top, 16, 16 );
			GUIStyle style = new GUIStyle();
			style.margin = new RectOffset( 0, 0, 0, 0 );
			style.border = new RectOffset( 0, 0, 0, 0 );
			return GUI.Button( position, _rightIcon, style );
		}

		void CreateNodeWindow( EditorData nodeData, float width, float height )
		{
			nodeData.rect.width = width;
			nodeData.rect.height = height;
			Rect resultRect = GUI.Window( nodeData.id, nodeData.rect, DrawNodeWindow, "" );

			// If the node was moved, save the change and mark the asset as dirty.
			if ( resultRect != nodeData.rect )
			{
				nodeData.rect = resultRect;
				EditorUtility.SetDirty( _behaviorTree );
			}
		}

		void DrawNodeWindow( int id )
		{
			// Get EditorNode
			EditorData nodeData = _editorData[id];
			TreeNode node = nodeData.node;

			Type selectedType = CreateNodeTypeDropdown( node );
			if ( selectedType != node.GetType() )
			{
				ReplaceNode( nodeData, selectedType );
				node = nodeData.node;
			}

			if ( node is Compositor )
			{
				Compositor compositor = (Compositor)node;

				Rect rect = new Rect( 0, 20, _nodeWidth, 20 );
				if ( GUI.Button( rect, "Add Child" ) )
				{
					TreeNode childNode = CreateNodeWithParent( typeof( NullNode ), compositor );
					compositor._children.Add( childNode );
				}
			}
			else if ( node is LeafNode )
			{
				Editor editor = Editor.CreateEditor( node );
				editor.OnInspectorGUI();
			}

			// Drag window last because otherwise you can't click on things
			GUI.DragWindow();
		}

		void DrawNodeCurve( Rect start, Rect end )
		{
			Vector3 startPos = new Vector3( start.x + start.width * 0.5f, start.y + start.height, 0 );
			Vector3 endPos = new Vector3( end.x + end.width * 0.5f, end.y, 0 );
			
			DrawNodeCurve( startPos, endPos );
		}

		/// <summary>
		/// Draws the bezier curve between two nodes, offsetting the starting node
		/// based on the number of children nodes and the index of the current child.
		/// </summary>
		/// <param name="start">A Rect describing the position of the starting node.</param>
		/// <param name="end">A Rect describing the position of the ending node.</param>
		/// <param name="index">The index of the end node in the parent node's children array.</param>
		/// <param name="count">The total number of children that the parent node has.</param>
		void DrawNodeCurve( Rect start, Rect end, int index, int count )
		{
			float incrementSize = start.width / count;
			float startX = start.x + incrementSize * index + incrementSize * 0.5f;
			Vector3 startPos = new Vector3( startX, start.y + start.height, 0 );
			Vector3 endPos = new Vector3( end.x + end.width * 0.5f, end.y, 0 );
			
			DrawNodeCurve( startPos, endPos );
		}

		void DrawNodeCurve( Vector3 startPos, Vector3 endPos )
		{
			// Calculate tangents.
			float dist = Vector3.Distance( startPos, endPos );
			float curvePower = dist * 0.25f;

			Vector3 startTan = startPos + Vector3.up * curvePower;
			Vector3 endTan = endPos + Vector3.down * curvePower;
			Color shadowCol = new Color( 0, 0, 0, 0.06f );
		
			// Draw a shadow.
			for ( int i = 0; i < 3; i++ )
			{
				Handles.DrawBezier( startPos, endPos, startTan, endTan, shadowCol, null, ( i + 1 ) * 5 );
			}

			Handles.DrawBezier( startPos, endPos, startTan, endTan, Color.black, null, 2 );
		}
		#endregion

		#region Create and Destroy Nodes
		// NOTE: Do NOT call this directly! It's a only helper function for the other CreateNode...() methods.
		TreeNode CreateNode( Type nodeType )
		{
			DebugUtils.Assert( _behaviorTree, "Can't create a node without an asset to add it to!" );

			TreeNode newNode = (TreeNode)ScriptableObject.CreateInstance( nodeType );
			newNode.id = newNode.GetInstanceID(); // generate a unique ID for the node.

			AssetDatabase.AddObjectToAsset( newNode, _behaviorTree );
			EditorUtility.SetDirty( _behaviorTree );

			return newNode;
		}

		TreeNode CreateNodeWithParent( Type nodeType, TreeNode parent )
		{
			TreeNode newNode = CreateNode( nodeType );
			_behaviorTree._editorData.Add( new EditorData( newNode, parent ) );
			BuildEditorData();
			return newNode;
		}

		TreeNode CreateNodeWithoutParent( Type nodeType )
		{
			TreeNode newNode = CreateNode( nodeType );
			_behaviorTree._editorData.Add( new EditorData( newNode, null ) );
			BuildEditorData();
			return newNode;
		}

		TreeNode CreateNodeWithExistingData( Type nodeType, EditorData nodeData )
		{
			TreeNode newNode = CreateNode( nodeType );

			// Use ID of new node, rather than reusing old node
			nodeData.id = newNode.id;

			// Give node data a reference to the new node
			nodeData.node = newNode;

			// Put new node back in the dict.
			_behaviorTree._editorData.Add( nodeData );
			BuildEditorData();

			return newNode;
		}
		
		void DeleteNode( TreeNode node )
		{
			if ( node is Decorator )
			{
				DeleteNode( ( (Decorator)node )._child );
			}
			else if ( node is Compositor )
			{
				foreach ( TreeNode child in ( (Compositor)node )._children )
				{
					DeleteNode( child );
				}
			}

			// Remove the editor data from the list held by the behavior tree.
			_behaviorTree._editorData.Remove( _editorData[node.id] );

			// Remove the data from the local dict.
			_editorData.Remove( node.id );

			DestroyImmediate( node, true );
		}

		void ReplaceNode( EditorData nodeData, Type newType )
		{
			DeleteNode( nodeData.node );
			CreateNodeWithExistingData( newType, nodeData );

			// Update parent's reference to the node.
			if ( nodeData.parent )
			{
				if ( nodeData.parent is Decorator )
				{
					Decorator parentDecorator = (Decorator)nodeData.parent;
					parentDecorator._child = nodeData.node;
				}
				else if ( nodeData.parent is Compositor )
				{
					Compositor parentCompositor = (Compositor)nodeData.parent;
					parentCompositor._children[nodeData.parentIndex] = nodeData.node;
				}
				else
				{
					Debug.LogError( "Parent node is neither a Decorator nor a Compositor!" );
				}
			}
			else
			{
				// Node is root node.
				_behaviorTree.root = nodeData.node;
			}
		}
		#endregion

		#region General Utilities
		void SwapCompositorChildren( Compositor compositor, int firstIndex, int secondIndex )
		{
			// Retrieve nodes and editor data.
			TreeNode first = compositor._children[firstIndex];
			EditorData childData = _editorData[first.id];

			TreeNode second = compositor._children[secondIndex];
			EditorData secondData = _editorData[second.id];

			// Swap the order of the children in the array.
			compositor._children[secondIndex] = first;
			compositor._children[firstIndex] = second;

			// Swap the positions of the two children's nodes.
			// TODO: Move the entire subtree under each node by the same amount.
			Vector2 tempPosition = secondData.rect.position;
			secondData.rect.position = childData.rect.position;
			childData.rect.position = tempPosition;

			// Update EditorData indices.
			secondData.parentIndex = firstIndex;
			childData.parentIndex = secondIndex;
		}
		#endregion
	}
}
#endif
namespace BehaviorTree
{
	[Serializable]
	public class EditorData
	{
		public int id = 0;
		public TreeNode node;
		public TreeNode parent; /// If this is null then the node is the root node.
		public int parentIndex = 0; /// The index of the node in the parent's _children array (only used if parent is a compositor).
		public Rect rect = new Rect( 10, 10, 100, 100 );
		public bool foldout = true;

		public EditorData( TreeNode node, TreeNode parent )
		{
			this.node = node;
			this.parent = parent;
			this.id = node.id;

			if ( parent is Compositor )
			{
				// EditorData is always created before adding the node
				// to the parent's list of children, so we can assume its
				// index with be _children.Count.
				parentIndex = ( (Compositor)parent )._children.Count;
			}
		}
	}
}
