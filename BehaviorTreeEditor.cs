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

	Dictionary<TreeNode, EditorNode> _editorData = new Dictionary<TreeNode,EditorNode>();

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
		DrawNode( _behaviorTree.root );
	}

	void DrawNode( TreeNode node )
	{
		// ensure that there's an editor node for this node
		if ( !_editorData.ContainsKey( node ) )
		{
			_editorData.Add( node, new EditorNode( node, null, _editorData.Count ) );
		}

		EditorNode nodeData = _editorData[node];

		BeginWindows();
		{
			nodeData.nodeRect = GUI.Window( nodeData.id, nodeData.nodeRect, DrawNodeWindow, DisplayName( node.ToString() ) );
		}
		EndWindows();
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
		EditorNode nodeData = _editorData.Values.First( editorNode => editorNode.id == id );
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
		if ( nodeData.parent )
		{
			// TODO: replace child node
		}
		else
		{
			// Node is root node
			_editorData.Remove( nodeData.node );
			DeleteNode( nodeData.node );
			nodeData.node = CreateNode( newType );
			_editorData.Add( nodeData.node, nodeData );
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

	public static void DeleteNode( TreeNode parentNode )
	{
		if ( parentNode is Decorator )
		{
			DeleteNode( ( (Decorator)parentNode )._child );
		}
		else if ( parentNode is Compositor )
		{
			foreach ( TreeNode child in ( (Compositor)parentNode )._children )
			{
				DeleteNode( child );
			}
		}

		DestroyImmediate( parentNode, true );
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
		EditorNode nodeData = _editorData[node];

		Rect rect = new Rect( 0, 0, 100, 20 );
		nodeData.foldout = EditorGUI.Foldout( rect, nodeData.foldout, "children", true );
		return nodeData.foldout;
	}

	public static TreeNode CreateNode( Type nodeType )
	{
		DebugUtils.Assert( _instance, "Cannot create a node without a current editor instance!" );
		DebugUtils.Assert( _instance._behaviorTree, "Can't create a node without an asset to add it to!" );

		TreeNode newNode = (TreeNode)ScriptableObject.CreateInstance( nodeType );
		AssetDatabase.AddObjectToAsset( newNode, _instance._behaviorTree );
		EditorUtility.SetDirty( _instance._behaviorTree );
		return newNode;
	}

	public static TreeNode nullNode
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

class EditorNode
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
#endif
