#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EditorUtils
{
    // 유니티 상단 메뉴바에 [Tools] -> [Clear All Saved Data] 메뉴를 생성합니다.
    [MenuItem("Tools/Clear All Saved Data (Reset Tokens)")]
    public static void ClearPlayerPrefs()
    {
        // PlayerPrefs에 저장된 토큰을 포함한 모든 데이터를 지웁니다.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        Debug.Log("<color=red><b>[성공] 유니티 에디터의 모든 로컬 데이터(세션 토큰 등)가 완전히 삭제되었습니다!</b></color>");
    }
}
#endif