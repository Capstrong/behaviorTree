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
			BehaviorTree behaviorTreeAsset = ScriptableObject.CreateInstance<BehaviorTree>();
			AssetDatabase.CreateAsset( behaviorTreeAsset, "Assets/NewBehaviorTree.asset" );
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

				CreateTypeLists();

				// only draw editor if a behavior tree is selected
				if ( _behaviorTree )
				{
					// super hacky thing
					if ( !_behaviorTree.root )
					{
						Debug.Log( "Behavior tree root doesn't exist, creating a new one." );
						_behaviorTree.root = CreateNodeWithoutParent( typeof( NullNode ) );
					}

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
			if ( !node )
			{
				Debug.LogError( "Node is null!" );
				return;
			}

			EditorData nodeData = _editorData[node.id]; // TODO: Crash here after the editor resets.

			// draw window
			Rect resultRect = GUI.Window( nodeData.id, nodeData.nodeRect, DrawNodeWindow, "" );
			if ( resultRect != nodeData.nodeRect )
			{
				nodeData.nodeRect = resultRect;
				EditorUtility.SetDirty( _behaviorTree );
			}

			// handle children
			if ( node is Decorator )
			{
				Decorator decorator = (Decorator)node;
			
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
			else if ( node is Compositor )
			{
				Compositor compositor = (Compositor)node;

				foreach ( TreeNode child in compositor._children )
				{
					EditorData childData = _editorData[child.id];

					// handle drawing child
					DrawNodeCurve( nodeData.nodeRect, childData.nodeRect );
					DrawNode( child );
				}
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
			}

			if ( node is Compositor )
			{
				Compositor compositor = (Compositor)node;

				Rect rect = new Rect( 0, 30, nodeData.nodeRect.width, 20 );
				if ( GUI.Button( rect, "Add Child" ) )
				{
					TreeNode childNode = CreateNodeWithParent( typeof( NullNode ), compositor );
					compositor._children.Add( childNode );
				}
			}

			// Drag window last because otherwise you can't click on things
			GUI.DragWindow();
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

		void CreateTypeLists()
		{
			// get list of possible node types
			// i.e., all types that extend TreeNode and are not abstract
			nodeTypes = typeof( TreeNode ).Assembly.GetTypes()
					.Where( type => type.IsSubclassOf( typeof( TreeNode ) ) && !type.IsAbstract ).ToArray<Type>();
			nodeTypeNames = Array.ConvertAll<Type, String>( nodeTypes,
				new Converter<Type, String> (
					delegate( Type type ) { return type.Name; } ) );
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

		Type CreateNodeTypeSelector( TreeNode node )
		{
			int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : Array.IndexOf<Type>( nodeTypes, typeof( NullNode ) ) );
			Rect rect = new Rect( 0, 0, 100, 20 );
			selectedType = EditorGUI.Popup( rect, selectedType, nodeTypeNames );
			return nodeTypes[selectedType];
		}

		bool CreateChildrenFoldout( TreeNode node, int height )
		{
			EditorData nodeData = _editorData[node.id];

			Rect rect = new Rect( 0, 0, 100, 20 );
			nodeData.foldout = EditorGUI.Foldout( rect, nodeData.foldout, "children", true );
			return nodeData.foldout;
		}

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

		TreeNode nullNode
		{
			get
			{
				return CreateNodeWithoutParent( typeof( NullNode ) );
			}
		}
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
		public TreeNode parent; // If this is null then the node is the root node.
		public int parentIndex = 0; // The index of the node in the parent's _children array (only used if parent is a compositor).
		public Rect nodeRect = new Rect( 10, 10, 100, 100 );
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
