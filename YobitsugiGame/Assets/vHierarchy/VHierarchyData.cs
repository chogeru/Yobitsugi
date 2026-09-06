#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static VHierarchy.Libs.VUtils;
using static VHierarchy.Libs.VGUI;

#if UNITY_6000_3_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<UnityEngine.EntityId>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<UnityEngine.EntityId>;
#elif UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif


#if UNITY_6000_3_OR_NEWER
using ObjectID = UnityEngine.EntityId;
#else
using ObjectID = System.Int32;
#endif



namespace VHierarchy
{
    public class VHierarchyData : ScriptableObject
    {
        public SerializeableDicitonary<string, SceneData> sceneDatasByGuid = new SerializeableDicitonary<string, SceneData>();
        public Dictionary<Scene, SceneData> sceneDatasByScene = new Dictionary<Scene, SceneData>();

        [System.Serializable]
        public class SceneData
        {
            public SerializeableDicitonary<string, GameObjectData> goDatasByGlobalId = new SerializeableDicitonary<string, GameObjectData>();
            public SerializeableDicitonary<ObjectID, GameObjectData> goDatasByInstanceId = new SerializeableDicitonary<ObjectID, GameObjectData>(); // serializable so prefabs don't loose their icons on playmode enter

        }

        [System.Serializable]
        public class GameObjectData
        {
            public Color color => VHierarchyIconEditor.GetColor(iColor);
            public int iColor;
            public string icon = "";

        }
    }
}
#endif