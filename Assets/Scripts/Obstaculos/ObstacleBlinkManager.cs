using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleBlinkManager : MonoBehaviour
{
    [System.Serializable]
    public class BlinkingObject
    {
        public GameObject targetObject;     
        public float showDuration = 3f;      
        public float hideDuration = 2f;      
        public float startDelay = 0f;       
        public bool startVisible = true;     
        
        [HideInInspector] public bool isVisible;
        [HideInInspector] public Coroutine blinkCoroutine;
    }
    
    [Header("Objetos Piscantes")]
    public BlinkingObject[] blinkingObjects;
    
    [Header("Controle Global")]
    public bool startAutomatically = true;
    public bool showDebugInfo = true;
    
    private Dictionary<GameObject, BlinkingObject> objectLookup = new Dictionary<GameObject, BlinkingObject>();
    
    void Start()
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.targetObject != null)
            {
                objectLookup[obj.targetObject] = obj;
                
                obj.isVisible = obj.startVisible;
                obj.targetObject.SetActive(obj.startVisible);
                
                if (startAutomatically)
                {
                    StartObjectBlinking(obj);
                }
            }
            else
            {
                Debug.LogWarning("Objeto nulo no ObstacleBlinkManager!");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"ObstacleBlinkManager iniciado com {blinkingObjects.Length} objetos");
        }
    }
    
    void StartObjectBlinking(BlinkingObject blinkingObj)
    {
        if (blinkingObj.blinkCoroutine != null)
        {
            StopCoroutine(blinkingObj.blinkCoroutine);
        }
        
        blinkingObj.blinkCoroutine = StartCoroutine(BlinkRoutine(blinkingObj));
    }
    
    IEnumerator BlinkRoutine(BlinkingObject blinkingObj)
    {
        if (blinkingObj.startDelay > 0)
        {
            yield return new WaitForSeconds(blinkingObj.startDelay);
        }
        
        while (true)
        {
            blinkingObj.isVisible = true;
            blinkingObj.targetObject.SetActive(true);
            
            if (showDebugInfo)
            {
                Debug.Log($"{blinkingObj.targetObject.name}: VISÍVEL por {blinkingObj.showDuration}s");
            }
            
            yield return new WaitForSeconds(blinkingObj.showDuration);
            
            blinkingObj.isVisible = false;
            blinkingObj.targetObject.SetActive(false);
            
            if (showDebugInfo)
            {
                Debug.Log($"{blinkingObj.targetObject.name}: INVISÍVEL por {blinkingObj.hideDuration}s");
            }
            
            yield return new WaitForSeconds(blinkingObj.hideDuration);
        }
    }
    
    
    public void StartAllBlinking()
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.targetObject != null)
            {
                StartObjectBlinking(obj);
            }
        }
        
        if (showDebugInfo) Debug.Log("Todos os objetos iniciados");
    }
    
    public void StopAllBlinking()
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.blinkCoroutine != null)
            {
                StopCoroutine(obj.blinkCoroutine);
                obj.blinkCoroutine = null;
            }
        }
        
        if (showDebugInfo) Debug.Log("Todos os objetos parados");
    }
    
    public void ShowAllObjects()
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.targetObject != null)
            {
                if (obj.blinkCoroutine != null)
                {
                    StopCoroutine(obj.blinkCoroutine);
                    obj.blinkCoroutine = null;
                }
                
                obj.isVisible = true;
                obj.targetObject.SetActive(true);
            }
        }
        
        if (showDebugInfo) Debug.Log("Todos os objetos VISÍVEIS");
    }
    
    public void HideAllObjects()
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.targetObject != null)
            {
                if (obj.blinkCoroutine != null)
                {
                    StopCoroutine(obj.blinkCoroutine);
                    obj.blinkCoroutine = null;
                }
                
                obj.isVisible = false;
                obj.targetObject.SetActive(false);
            }
        }
        
        if (showDebugInfo) Debug.Log("Todos os objetos INVISÍVEIS");
    }
    
    public void ToggleObject(GameObject obj)
    {
        if (objectLookup.ContainsKey(obj))
        {
            BlinkingObject blinkingObj = objectLookup[obj];
            
            if (blinkingObj.blinkCoroutine != null)
            {
                StopCoroutine(blinkingObj.blinkCoroutine);
                blinkingObj.blinkCoroutine = null;
            }
            
            blinkingObj.isVisible = !blinkingObj.isVisible;
            obj.SetActive(blinkingObj.isVisible);
            
            if (showDebugInfo)
            {
                Debug.Log($"{obj.name}: alternado para {(blinkingObj.isVisible ? "VISÍVEL" : "INVISÍVEL")}");
            }
        }
    }
    
    public void StartObject(GameObject obj)
    {
        if (objectLookup.ContainsKey(obj))
        {
            StartObjectBlinking(objectLookup[obj]);
        }
    }
    
    public void StopObject(GameObject obj)
    {
        if (objectLookup.ContainsKey(obj))
        {
            BlinkingObject blinkingObj = objectLookup[obj];
            
            if (blinkingObj.blinkCoroutine != null)
            {
                StopCoroutine(blinkingObj.blinkCoroutine);
                blinkingObj.blinkCoroutine = null;
            }
        }
    }
    
    
    public void StartObjectByName(string objectName)
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.targetObject != null && obj.targetObject.name == objectName)
            {
                StartObjectBlinking(obj);
                return;
            }
        }
    }
    
    public void StopObjectByName(string objectName)
    {
        foreach (BlinkingObject obj in blinkingObjects)
        {
            if (obj.targetObject != null && obj.targetObject.name == objectName && obj.blinkCoroutine != null)
            {
                StopCoroutine(obj.blinkCoroutine);
                obj.blinkCoroutine = null;
                return;
            }
        }
    }
    

}