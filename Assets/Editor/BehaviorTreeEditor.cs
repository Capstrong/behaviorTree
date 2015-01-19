using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

public class BehaviorTreeEditor : EditorWindow
{
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
		_behaviorTree = (BehaviorTree)EditorGUILayout.ObjectField( "Behavior Tree",
		                                                           _behaviorTree,
		                                                           typeof( BehaviorTree ),
		                                                           false );

		if ( _behaviorTree != null )
		{
			Type[] nodeTypes = typeof( TreeNode ).Assembly.GetTypes()
				.Where( type => type.IsSubclassOf( typeof( TreeNode ) ) && !type.IsAbstract ).ToArray<Type>();
			String[] nodeTypeNames = Array.ConvertAll<Type, String>( nodeTypes,
				new Converter<Type, String> (
					delegate( Type type ) { return type.ToString(); } ) );

			GUILayout.Label( "Nodes", EditorStyles.boldLabel );
			int selectedType = ( _behaviorTree.root != null ? Array.IndexOf<Type>( nodeTypes, _behaviorTree.root.GetType() ) : 0 );
			selectedType = EditorGUILayout.Popup( "Root Node", selectedType, nodeTypeNames );
			_behaviorTree.root = (TreeNode)Activator.CreateInstance( nodeTypes[selectedType] );
		}
	}
}
