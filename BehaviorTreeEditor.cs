#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

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
			CreateGUI();
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
		if ( _behaviorTree && !_behaviorTree.root )
		{
			_behaviorTree.root = nullNode;
		}

		if ( _behaviorTree != null )
		{
			GUILayout.Label( "Nodes", EditorStyles.boldLabel );

			Type resultType = CreateNodeTypeSelector( _behaviorTree.root );
			if ( resultType != _behaviorTree.root.GetType() )
			{
				_behaviorTree.root = (TreeNode)ScriptableObject.CreateInstance( resultType );
			}
			_behaviorTree.root.OnGUI();
		}
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

	public static Type CreateNodeTypeSelector( TreeNode node )
	{
		int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : 0 );
		selectedType = EditorGUILayout.Popup( "<---->", selectedType, nodeTypeNames );
		return nodeTypes[selectedType];
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
}
#endif
