//using UnityEditor;
//using Unity.Netcode;
//using UnityEditor.SceneManagement;

//namespace EditorTool
//{
//    /// <summary>
//    /// Fix network ID duplicate issue
//    /// </summary>

//    public class FixNetworkID : ScriptableWizard
//    {
//        [MenuItem("Netcode/Fix Network IDs", priority = 500)]
//        static void ScriptableWizardMenu()
//        {
//            ScriptableWizard.DisplayWizard<FixNetworkID>("Fix Network IDs", "Fix");
//        }

//        void OnWizardCreate()
//        {
//            NetworkObject[] nobjs = FindObjectsOfType<NetworkObject>();
//            foreach (NetworkObject nobj in nobjs)
//            {
//                EditorUtility.SetDirty(nobj);
//            }

//            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
//        }

//        void OnWizardUpdate()
//        {
//            helpString = "Use this tool when you receive error that there are duplicate NetworkObject ids.";
//        }
//    }
//}

