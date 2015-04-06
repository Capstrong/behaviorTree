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

		void DrawNode( TreeNode node )
		{
			if ( node == null )
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

			// handle drawing the child
			DrawNodeCurve( nodeData.nodeRect, childData.nodeRect );
			DrawNode( decorator._child );
		}

		void DrawCompositor( Compositor compositor )
		{
			EditorData nodeData = _editorData[compositor.id];
			CreateNodeWindow( nodeData, _nodeWidth, _compositorHeight );

			foreach ( TreeNode child in compositor._children )
			{
				EditorData childData = _editorData[child.id];

				// handle drawing child
				DrawNodeCurve( nodeData.nodeRect, childData.nodeRect );
				DrawNode( child );
			}
		}

		void DrawLeaf( LeafNode leaf )
		{
			EditorData nodeData = _editorData[leaf.id];
			CreateNodeWindow( nodeData, _nodeWidth, _leafHeight );
		}

		static string DisplayName( string name )
		{
			if ( name.Contains( '.' ) )
			{
				string[] tokens = name.Split( '.' );
				return tokens[tokens.Length - 1];
			}
			return name;
		}

		void CreateTypeLists()
		{
			// get list of possible node types
			// i.e., all types that extend TreeNode and are not abstract
			nodeTypes = typeof( TreeNode ).Assembly.GetTypes()
					.Where( type => type.IsSubclassOf( typeof( TreeNode ) ) && !type.IsAbstract ).ToArray<Type>();
			nodeTypeNames = Array.ConvertAll<Type, String>( nodeTypes,
				new Converter<Type, String> (
					delegate( Type type ) { return DisplayName( type.ToString() ); } ) );
		}

		#region GUI Helpers
		public Type CreateNodeTypeSelector( TreeNode node )
		{
			int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : Array.IndexOf<Type>( nodeTypes, typeof( NullNode ) ) );
			Rect rect = new Rect( 0, 0, _nodeWidth - 40.0f, 20.0f );
			selectedType = EditorGUI.Popup( rect, selectedType, nodeTypeNames );
			return nodeTypes[selectedType];
		}

		void CreateNodeWindow( EditorData nodeData, float width, float height )
		{
			nodeData.nodeRect.width = width;
			nodeData.nodeRect.height = height;
			Rect resultRect = GUI.Window( nodeData.id, nodeData.nodeRect, DrawNodeWindow, "" );

			// If the node was moved, save the change and mark the asset as dirty.
			if ( resultRect != nodeData.nodeRect )
			{
				nodeData.nodeRect = resultRect;
				EditorUtility.SetDirty( _behaviorTree );
			}
		}

		void DrawNodeWindow( int id )
		{
			// Get EditorNode
			EditorData nodeData = _editorData[id];
			TreeNode node = nodeData.node;

			Type selectedType = CreateNodeTypeSelector( node );
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
			Vector3 startTan = startPos + Vector3.up * 50;
			Vector3 endTan = endPos + Vector3.down * 50;
			Color shadowCol = new Color( 0, 0, 0, 0.06f );
		
			// Draw a shadow
			for ( int i = 0; i < 3; i++ )
			{
				Handles.DrawBezier( startPos, endPos, startTan, endTan, shadowCol, null, ( i + 1 ) * 5 );
			}

			Handles.DrawBezier( startPos, endPos, startTan, endTan, Color.black, null, 1 );
		}
		#endregion

		#region Create and Destroy Nodes
		// NOTE: Do NOT call this directly! It's a only helper function for the other CreateNode...() methods.
		public TreeNode CreateNode( Type nodeType )
		{
			DebugUtils.Assert( _behaviorTree, "Can't create a node without an asset to add it to!" );

			TreeNode newNode = (TreeNode)ScriptableObject.CreateInstance( nodeType );
			newNode.id = newNode.GetInstanceID(); // generate a unique ID for the node.

			AssetDatabase.AddObjectToAsset( newNode, _behaviorTree );
			EditorUtility.SetDirty( _behaviorTree );

			return newNode;
		}

		public TreeNode CreateNodeWithParent( Type nodeType, TreeNode parent )
		{
			TreeNode newNode = CreateNode( nodeType );
			_behaviorTree._editorData.Add( new EditorData( newNode, parent ) );
			BuildEditorData();
			return newNode;
		}

		public TreeNode CreateNodeWithoutParent( Type nodeType )
		{
			TreeNode newNode = CreateNode( nodeType );
			_behaviorTree._editorData.Add( new EditorData( newNode, null ) );
			BuildEditorData();
			return newNode;
		}

		public TreeNode CreateNodeWithExistingData( Type nodeType, EditorData nodeData )
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
		
		public void DeleteNode( TreeNode node )
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

		public static BehaviorTree CloneTree( BehaviorTree behaviorTree )
		{
			BehaviorTree treeClone = Instantiate<BehaviorTree>( behaviorTree );
			treeClone.root = CloneNode( behaviorTree.root );
			return treeClone;
		}

		public static TreeNode CloneNode( TreeNode node )
		{
			TreeNode nodeClone = Instantiate<TreeNode>( node );

			if ( node is Decorator )
			{
				( (Decorator)nodeClone )._child = CloneNode( ( (Decorator)node )._child );
			}
			else if ( node is Compositor )
			{
				List<TreeNode> cloneChildren = new List<TreeNode>();
				foreach ( TreeNode child in ( (Compositor)node )._children )
				{
					cloneChildren.Add( (TreeNode)CloneNode( child ) );
				}

				( (Compositor)nodeClone )._children = cloneChildren;
			}

			return nodeClone;
		}
	}
}
#endif
namespace BehaviorTree
{
	[Serializable]
	public class EditorData
	{
		// NOTE: This should not be called directly.
		//public EditorData() { }

		public EditorData( TreeNode node, TreeNode parent )
		{
			this.node = node;
			this.parent = parent;
			this.id = node.id;

			if ( parent is Compositor )
			{
				parentIndex = ( (Compositor)parent )._children.Count;
			}
		}

		public int id = 0;
		public TreeNode node;
		public TreeNode parent; // If this is null then the node is the root node.
		public int parentIndex = 0; // The index of the node in the parent's _children array (only used if parent is a compositor).
		public Rect nodeRect = new Rect( 10, 10, 100, 100 );
		public bool foldout = true;
	}
}
