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

	/**
	 * @brief Add a menu item in the editor for creating new behavior tree assets.
	 *
	 * @todo
	 *     Create new asset in currently selected folder of the project view,
	 *     rather than always placing them in the root Assets folder.
	 *
	 * @todo
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
		CreateTypeLists();

		_behaviorTree = (BehaviorTree)EditorGUILayout.ObjectField( "Behavior Tree",
		                                                           _behaviorTree,
		                                                           typeof( BehaviorTree ),
		                                                           false );

		if ( _behaviorTree != null )
		{
			GUILayout.Label( "Nodes", EditorStyles.boldLabel );
			
			Type resultType = CreateNodeTypeSelector( _behaviorTree.root );
			if ( resultType != _behaviorTree.root.GetType() )
			{
				_behaviorTree.root = (TreeNode)Activator.CreateInstance( resultType );
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

	public static Type CreateNodeTypeSelector( TreeNode node )
	{
		int selectedType = ( node != null ? Array.IndexOf<Type>( nodeTypes, node.GetType() ) : 0 );
		selectedType = EditorGUILayout.Popup( "<---->", selectedType, nodeTypeNames );
		return nodeTypes[selectedType];
	}

	public static TreeNode CreateNode( Type nodeType )
	{
		return (TreeNode)Activator.CreateInstance( nodeType );
	}
}
