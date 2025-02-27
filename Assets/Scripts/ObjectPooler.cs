﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ObjectPooler : MonoBehaviour
{

    public static ObjectPooler Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("more than one gameManager");
            return;
        }
        Instance = this;

     
    }

    //오브젝트 풀러는 모든 인스턴스에서 실행되어야한다.(마스터 클라이언트에서 만들고 다른 인스턴스에서 사용하는 것이 아님)

    [System.Serializable]
    public class Pool
    {   
        //태그를 쓰든 안쓰든 상관은 없다. 프리팹의 이름으로 풀을 식별하면 되니까. 편의를 위해 태그를 쓴다.
        public string tag;
        public GameObject prefab;
        //최대 몇개까지 게임에 enable할 수 있는가.
        public int size;
    }

   
    public List<Pool> pools;

    //큐를 쓰는 이유는 꺼낼때 빠르기 떄문이다.(스택과는 다르게 인간이 볼때 합리적인 순서로 저장되는 자료구조이기도하고) List는 인덱스로 꺼내고, 큐는 그냥 꺼내기 떄문에 더 빠름.
    Dictionary<string, Queue<GameObject>> poolDictionary;

    public event System.Action onObjectPoolReady;
    public bool IsPoolReady { get; private set; }


    // Start is called before the first frame update
    void Start()
    {
        IsPoolReady = false;

        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            //프리팹의 이름을 풀의 태그로 한다.
            pool.tag = pool.prefab.name;

            //각각의 pool을 pools에서 옮길 큐. 오브젝트 풀 하나는 Queue<GameObject>로 표현된다. 
            Queue<GameObject> objectPool = new Queue<GameObject>();

            //생성된 풀을 인스펙터 상에서 정리하기 위해
            GameObject parent = new GameObject(pool.tag + "Pool");

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity);
                //스크립트 실행안되게 false (onEnable은 실행됨)
                obj.SetActive(false);
                //인스펙터창 정리하기 위해 묶음
                obj.transform.SetParent(parent.transform);
                //생성한 인스턴스에 대한 레퍼런스를 pool에 넣는다.
                objectPool.Enqueue(obj);
            }

            if(!poolDictionary.ContainsKey(pool.tag))
                poolDictionary.Add(pool.tag, objectPool);

        }

        onObjectPoolReady?.Invoke();
        IsPoolReady = true;

    }


    public GameObject Instantiate(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("The tag" + tag + "is not exist in poolDictionary");
            return null;
        }


        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        //재사용하기 위해 다시 레퍼런스를 해당 풀에 넣는다.  
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;


    }

    public void Destroy(GameObject go, float delay = 0f)
    {
        StartCoroutine(DestroyCorutine(go, delay));
    }

    private IEnumerator DestroyCorutine(GameObject go, float delay)
    {

        //지연한다음
        yield return new WaitForSeconds(delay);
        Rigidbody rib = go.GetComponent<Rigidbody>();
        if (rib != null)
        {
            rib.velocity = Vector3.zero;
        }
        //비활성화
        go.SetActive(false);
    }
}
