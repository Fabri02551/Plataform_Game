using UnityEngine;
using System.Collections;

// Este atributo asegura que el objeto al que se adjunta este script tambi�n tenga un BoxCollider2D.
[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
    public LayerMask collisionMask; // M�scara de capa que especifica con qu� capas colisionar�n los rayos.

    public const float skinWidth = .015f; // Un peque�o margen utilizado para evitar problemas de colisi�n.
    const float dstBetweenRays = .25f; // Distancia entre los rayos para la detecci�n de colisiones.

    // Variables para contar la cantidad de rayos que se lanzar�n en las direcciones horizontal y vertical.
    [HideInInspector]
    public int horizontalRayCount;
    [HideInInspector]
    public int verticalRayCount;

    // Variables que determinan el espacio entre los rayos en las direcciones horizontal y vertical.
    [HideInInspector]
    public float horizontalRaySpacing;
    [HideInInspector]
    public float verticalRaySpacing;

    [HideInInspector]
    public BoxCollider2D collider; // Referencia al componente BoxCollider2D.
    public RaycastOrigins raycastOrigins; // Estructura que almacena los or�genes de los rayos.

    // M�todo virtual para inicializar el componente BoxCollider2D. Puede ser sobrescrito en clases derivadas.
    public virtual void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    // M�todo virtual que se ejecuta al inicio. Calcula el espaciado de los rayos. Puede ser sobrescrito en clases derivadas.
    public virtual void Start()
    {
        CalculateRaySpacing();
    }

    // Actualiza los or�genes de los rayos en funci�n de la posici�n y tama�o actual del collider.
    public void UpdateRaycastOrigins()
    {
        // Se ajustan los l�mites del collider utilizando el skinWidth para evitar colisiones innecesarias.
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Se establecen los puntos de origen de los rayos en las esquinas del collider.
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    // Calcula el espaciado entre los rayos en funci�n del tama�o del collider y la distancia definida entre los rayos.
    public void CalculateRaySpacing()
    {
        // Se ajustan los l�mites del collider utilizando el skinWidth.
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Se obtienen las dimensiones del collider.
        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        // Se calculan las cantidades de rayos necesarios para cubrir las dimensiones del collider.
        horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

        // Se calculan los espacios entre los rayos para asegurar una cobertura uniforme.
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    // Estructura que almacena las coordenadas de los or�genes de los rayos.
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;     // Esquinas superiores izquierda y derecha del collider.
        public Vector2 bottomLeft, bottomRight; // Esquinas inferiores izquierda y derecha del collider.
    }
}
