//using System.Collections;
//using System.Collections.Generic;
//using Unity.Entities;
//using UnityEngine;

//public class ResourcePrefabAuthoring : MonoBehaviour
//{
//    public ResourceArrayType restype;
//    public bool test_prefab;
//    public GameObject prefab;
//    public int dbg_array_index;
//    private void OnDrawGizmos()
//    {
//        dbg_array_index = transform.GetSiblingIndex();
//    }
//    public class Bakery : Baker<ResourcePrefabAuthoring>
//    {
//        public override void Bake(ResourcePrefabAuthoring authoring)
//        {
//            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
//            if (authoring.test_prefab)
//            {

//                var baked = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);
//                AddComponent(entity, new ResourcePrefab() { array_type = authoring.restype, array_index = authoring.transform.GetSiblingIndex(), baked_prefab = baked });
//                //Debug.Log("offline" + entity.ToString() + ", " + baked.ToString());
//                //entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
//                //AddComponent(entity, new ResourcePrefab() { array_type = authoring.restype, array_index = authoring.transform.GetSiblingIndex() });
//            }
//            else
//            {
//                //entity = GetEntity(TransformUsageFlags.Dynamic);
//                AddComponent(entity, new ResourcePrefab() { array_type = authoring.restype, array_index = authoring.transform.GetSiblingIndex() });
//            }
//            //Debug.Log("baked " + authoring.restype.ToString());
//        }
//    }
//}