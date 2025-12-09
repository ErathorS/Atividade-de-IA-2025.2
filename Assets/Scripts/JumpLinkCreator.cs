using UnityEngine;
using UnityEngine.AI;

public class JumpLinkCreator : MonoBehaviour
{
    [Header("Configuração do Pulo")]
    public Transform jumpStartPoint;    // Ponto antes do pulo (waypoint 3)
    public Transform jumpEndPoint;      // Ponto depois do pulo (plataforma 3)
    public float jumpCost = 5f;         // Custo do pulo (maior = menos preferível)
    
    void Start()
    {
        CreateJumpLink();
    }
    
    void CreateJumpLink()
    {
        if (jumpStartPoint == null || jumpEndPoint == null)
        {
            Debug.LogError("Pontos de início/fim do pulo não configurados!");
            return;
        }
        
        // Cria o GameObject do link
        GameObject linkObject = new GameObject("BossJumpLink");
        linkObject.transform.SetParent(transform);
        
        // Adiciona componente OffMeshLink
        OffMeshLink offMeshLink = linkObject.AddComponent<OffMeshLink>();
        
        // Configura os pontos
        offMeshLink.startTransform = jumpStartPoint;
        offMeshLink.endTransform = jumpEndPoint;
        
        // Configurações do link
        offMeshLink.activated = true;
        offMeshLink.costOverride = jumpCost;
        offMeshLink.biDirectional = false; // Só vai do start para o end
        offMeshLink.area = 2; // Área "Jump" (se configurada no Navigation Window)
        
        Debug.Log($"OffMeshLink criado: {jumpStartPoint.name} -> {jumpEndPoint.name}");
        
        // Adiciona Gizmos visuais
        linkObject.AddComponent<JumpLinkVisualizer>();
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (jumpStartPoint != null && jumpEndPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(jumpStartPoint.position, 0.3f);
            Gizmos.DrawSphere(jumpEndPoint.position, 0.3f);
            
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawLine(jumpStartPoint.position, jumpEndPoint.position);
            
            // Desenha seta
            Vector3 direction = (jumpEndPoint.position - jumpStartPoint.position).normalized;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 45, 0) * Vector3.back;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -45, 0) * Vector3.back;
            
            Gizmos.DrawLine(jumpEndPoint.position, jumpEndPoint.position - right * 0.5f);
            Gizmos.DrawLine(jumpEndPoint.position, jumpEndPoint.position - left * 0.5f);
        }
    }
    #endif
}

// Script auxiliar para visualização
public class JumpLinkVisualizer : MonoBehaviour
{
    void OnDrawGizmos()
    {
        OffMeshLink link = GetComponent<OffMeshLink>();
        if (link != null && link.startTransform != null && link.endTransform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(link.startTransform.position, link.endTransform.position);
            
            Gizmos.DrawWireSphere(link.startTransform.position, 0.4f);
            Gizmos.DrawWireSphere(link.endTransform.position, 0.4f);
        }
    }
}