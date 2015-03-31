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

	private const int _itemHeight = 20;
	private const int _indentSize = 10;
	private const int _foldoutWidth = 80;

	private int _indentLevel = 0;

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
			CreateGUI();
			AssetDatabase.SaveAssets();
		}
		catch ( Exception e )
		{
			Debug.LogError( e );
		}
	}

	void CreateGUI()
	{
		CreateTypeLists();

		_behaviorTree =
		    (BehaviorTree)EditorGUILayout.ObjectField(
		        "Behavior Tree",
		        _behaviorTree,
		        typeof( BehaviorTree ),
		        false );

		if ( _behaviorTree != null )
		{
			// set default root node
			if ( !_behaviorTree.root )
			{
				_behaviorTree.root = nullNode;
			}

			GUILayout.Label( "Nodes", EditorStyles.boldLabel );

			Type resultType = CreateNodeTypeSelector( _behaviorTree.root, 2 );
			if ( resultType != _behaviorTree.root.GetType() )
			{
				DeleteNode( _behaviorTree.root );
				_behaviorTree.root = CreateNode( resultType );
			}
			
			CreateNodeGUI( _behaviorTree.root, 3 );
		}
	}

	int CreateNodeGUI( TreeNode node, int height )
	{
		DebugUtils.Assert( node, "Cannot create GUI for null node!" );

		++_indentLevel;
		
		if ( node is Decorator )
		{
			height = CreateDecoratorGUI( (Decorator)node, height );
		}
		else if ( node is Compositor )
		{
			height = CreateCompositorGUI( (Compositor)node, height );
		}

		--_indentLevel;

		return height;
	}

	int CreateDecoratorGUI( Decorator node, int height )
	{
		if ( CreateChildrenFoldout( node, height ) )
		{
			Type resultType = CreateNodeTypeSelector( node._child, height );
			if ( node._child )
			{
				if ( resultType != node._child.GetType() )
				{
					BehaviorTreeEditor.DeleteNode( node._child );
					node._child = BehaviorTreeEditor.CreateNode( resultType );
				}
			}
			else
			{
				node._child = BehaviorTreeEditor.CreateNode( resultType );
			}

			return CreateNodeGUI( node._child, ++height );
		}
		else
		{
			return height;
		}
	}

	int CreateCompositorGUI( Compositor node, int height )
	{
		if ( CreateChildrenFoldout( node, height ) )
		{
			for ( int childIndex = 0; childIndex < node._children.Count; ++childIndex )
			{
				Type resultType = CreateNodeTypeSelector( node._children[childIndex], height );
				if ( resultType != node._children[childIndex].GetType() )
				{
					BehaviorTreeEditor.DeleteNode( node._children[childIndex] );
					node._children[childIndex] = BehaviorTreeEditor.CreateNode( resultType );
				}

				height = CreateNodeGUI( node._children[childIndex], ++height );
			}

			Rect rect = new Rect( _indentLevel * _indentSize + _foldoutWidth, height * _itemHeight, 200, _itemHeight - 5 );
			if ( GUI.Button( rect, "Add Child" ) )
			{
				node._children.Add( BehaviorTreeEditor.nullNode );
			}
			++height;
		}

		return height;
	}

	void CreateTypeLists()
	{
		// get list of possible node types
		// i.e., all types that extend TreeNode and are not abstract
		nodeTypes = typeof( TreeNode ).Assembly.GetTypes()
				.Where( type => type.IsSubclassOf( typeof( TreeNode ) ) && !type.IsAbstract ).ToArray<Type>();
		nodeTypeNames = Array.ConvertAll<Type, String>( nodeTypes,
			new Converter<Type, String> (
				delegate( Type type ) { return type.ToString(); } ) );
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

	public Type CreateNodeTypeSelector( TreeNode node, int height )
	{
		int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : Array.IndexOf<Type>( nodeTypes, typeof( NullNode ) ) );
		Rect rect = new Rect( _indentLevel * _indentSize + _foldoutWidth, height * _itemHeight, 200, _itemHeight );
		selectedType = EditorGUI.Popup( rect, selectedType, nodeTypeNames );
		return nodeTypes[selectedType];
	}

	public bool CreateChildrenFoldout( TreeNode node, int height )
	{
		if ( !_editorData.ContainsKey( node ) )
		{
			_editorData.Add( node, new EditorNode() );
		}

		EditorNode nodeData = _editorData[node];

		Rect rect = new Rect( _indentLevel * _indentSize, ( height - 1 ) * _itemHeight, _foldoutWidth, _itemHeight );
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
	public bool foldout = true;
}
}
#endif
