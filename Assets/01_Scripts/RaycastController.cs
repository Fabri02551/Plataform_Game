using UnityEngine;
using System.Collections;

// Este atributo asegura que el objeto al que se adjunta este script también tenga un BoxCollider2D.
[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
    public LayerMask collisionMask; // Máscara de capa que especifica con qué capas colisionarán los rayos.

    public const float skinWidth = .015f; // Un pequeño margen utilizado para evitar problemas de colisión.
    const float dstBetweenRays = .25f; // Distancia entre los rayos para la detección de colisiones.

    // Variables para contar la cantidad de rayos que se lanzarán en las direcciones horizontal y vertical.
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
    public RaycastOrigins raycastOrigins; // Estructura que almacena los orígenes de los rayos.

    // Método virtual para inicializar el componente BoxCollider2D. Puede ser sobrescrito en clases derivadas.
    public virtual void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    // Método virtual que se ejecuta al inicio. Calcula el espaciado de los rayos. Puede ser sobrescrito en clases derivadas.
    public virtual void Start()
    {
        CalculateRaySpacing();
    }

    // Actualiza los orígenes de los rayos en función de la posición y tamaño actual del collider.
    public void UpdateRaycastOrigins()
    {
        // Se ajustan los límites del collider utilizando el skinWidth para evitar colisiones innecesarias.
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Se establecen los puntos de origen de los rayos en las esquinas del collider.
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    // Calcula el espaciado entre los rayos en función del tamaño del collider y la distancia definida entre los rayos.
    public void CalculateRaySpacing()
    {
        // Se ajustan los límites del collider utilizando el skinWidth.
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

    // Estructura que almacena las coordenadas de los orígenes de los rayos.
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;     // Esquinas superiores izquierda y derecha del collider.
        public Vector2 bottomLeft, bottomRight; // Esquinas inferiores izquierda y derecha del collider.
    }
}
