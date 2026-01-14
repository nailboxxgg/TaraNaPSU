using UnityEngine;

namespace Core.Config
{
    [CreateAssetMenu(fileName = "FirebaseConfig", menuName = "Config/FirebaseConfig")]
    public class FirebaseConfig : ScriptableObject
    {
        [Header("Firestore Settings")]
        public string ProjectId = "taranapsu";
        public string CollectionId = "rooms";
        public string ApiKey = "AIzaSyB6EJS8m_8qkESYEACpXLGQa6BLHJH8X0A"; // Optional if using public read access

        public string GetFirestoreUrl()
        {
            return $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/sync/latest";
        }
    }
}
