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
	private static BehaviorTreeEditor _instance
	{
		get
		{
			return EditorWindow.GetWindow<BehaviorTreeEditor>();
		}
	}

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
					_behaviorTree.root = CreateNode( typeof( NullNode ) );
				}

				CreateGUI();

				AssetDatabase.SaveAssets();
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
		}

		EditorNode nodeData = _behaviorTree._editorData[node];

		// draw window
		nodeData.nodeRect = GUI.Window( nodeData.id, nodeData.nodeRect, DrawNodeWindow, DisplayName( node.ToString() ) );

		// handle children
		if ( node is Decorator )
		{
			Decorator decorator = (Decorator)node;
			
			// ensure that the node has a child
			if ( !decorator._child )
			{
				decorator._child = CreateNode( typeof( NullNode ), decorator );
			}

			EditorNode childData = _behaviorTree._editorData[decorator._child];

			// handle drawing the child
			DrawNodeCurve( nodeData.nodeRect, childData.nodeRect );

			DrawNode( decorator._child );
		}
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

	void DrawNodeWindow( int id )
	{
		// Get EditorNode
		EditorNode nodeData = _behaviorTree._editorData.Values.First( editorNode => editorNode.id == id );
		TreeNode node = nodeData.node;

		Type selectedType = CreateNodeTypeSelector( node );
		if ( selectedType != node.GetType() )
		{
			ReplaceNode( nodeData, selectedType );
		}

		// Drag window last because otherwise you can't click on things
		GUI.DragWindow();
	}

	void ReplaceNode( EditorNode nodeData, Type newType )
	{
		DeleteNode( nodeData.node );

		if ( nodeData.parent )
		{
			// TODO: Replace child node.
		}
		else
		{
			// Node is root node.
			nodeData.node = CreateNode( newType, nodeData );
			_behaviorTree.root = nodeData.node;
		}
	}

	void DrawNodeCurve( Rect start, Rect end )
	{
		Vector3 startPos = new Vector3( start.x + start.width, start.y + start.height / 2, 0 );
		Vector3 endPos = new Vector3( end.x, end.y + end.height / 2, 0 );
		Vector3 startTan = startPos + Vector3.right * 50;
		Vector3 endTan = endPos + Vector3.left * 50;
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
				delegate( Type type ) { return DisplayName( type.ToString() ); } ) );
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

		_behaviorTree._editorData.Remove( node );
		DestroyImmediate( node, true );
	}

	public Type CreateNodeTypeSelector( TreeNode node )
	{
		int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : Array.IndexOf<Type>( nodeTypes, typeof( NullNode ) ) );
		Rect rect = new Rect( 0, 0, 100, 20 );
		selectedType = EditorGUI.Popup( rect, selectedType, nodeTypeNames );
		return nodeTypes[selectedType];
	}

	public bool CreateChildrenFoldout( TreeNode node, int height )
	{
		EditorNode nodeData = _behaviorTree._editorData[node];

		Rect rect = new Rect( 0, 0, 100, 20 );
		nodeData.foldout = EditorGUI.Foldout( rect, nodeData.foldout, "children", true );
		return nodeData.foldout;
	}

	public TreeNode CreateNode( Type nodeType )
	{
		DebugUtils.Assert( _instance, "Cannot create a node without a current editor instance!" );
		DebugUtils.Assert( _instance._behaviorTree, "Can't create a node without an asset to add it to!" );

		TreeNode newNode = (TreeNode)ScriptableObject.CreateInstance( nodeType );
		AssetDatabase.AddObjectToAsset( newNode, _instance._behaviorTree );
		EditorUtility.SetDirty( _instance._behaviorTree );
		
		_behaviorTree._editorData.Add( newNode, new EditorNode( newNode, null, _behaviorTree._editorData.Count ) );

		return newNode;
	}

	public TreeNode CreateNode( Type nodeType, TreeNode parent )
	{
		TreeNode newNode = CreateNode( nodeType );
		_behaviorTree._editorData[newNode] = new EditorNode( newNode, parent, _behaviorTree._editorData.Count );
		return newNode;
	}

	public TreeNode CreateNode( Type nodeType, EditorNode nodeData )
	{
		TreeNode newNode = CreateNode( nodeType, nodeData.parent );
		nodeData.node = newNode;
		_behaviorTree._editorData[newNode] = nodeData;

		return newNode;
	}

	public TreeNode nullNode
	{
		get
		{
			return CreateNode( typeof( NullNode ) );
		}
	}

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
public class EditorNode
{
	public EditorNode( TreeNode node, TreeNode parent, int id )
	{
		this.node = node;
		this.parent = parent;
		this.id = id;
	}

	public int id = 0;
	public TreeNode node;
	public TreeNode parent; // If this is null then the node is the root node.
	public int parentIndex = 0; // The index of the node in the parent's _children array (only used if parent is a compositor).
	public Rect nodeRect = new Rect( 10, 10, 100, 100 );
	public bool foldout = true;
}
}
