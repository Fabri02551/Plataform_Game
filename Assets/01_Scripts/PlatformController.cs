using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformController : RaycastController
{
    public LayerMask passengerMask; // M�scara de capa para identificar a los pasajeros.

    public Vector3[] localWaypoints; // Puntos de referencia locales que definen el recorrido de la plataforma.
    Vector3[] globalWaypoints; // Puntos de referencia globales calculados a partir de los locales.

    public float speed; // Velocidad de la plataforma.
    public bool cyclic; // Indica si la plataforma debe moverse en un ciclo (de regreso al primer punto al final).
    public float waitTime; // Tiempo de espera en cada punto de referencia antes de continuar.
    [Range(0, 2)]
    public float easeAmount; // Cantidad de suavizado en el movimiento de la plataforma.

    int fromWaypointIndex; // �ndice del punto de referencia desde el que se est� moviendo la plataforma.
    float percentBetweenWaypoints; // Porcentaje del camino recorrido entre dos puntos de referencia.
    float nextMoveTime; // Tiempo en el que la plataforma se mover� nuevamente.

    List<PassengerMovement> passengerMovement; // Lista para almacenar el movimiento de los pasajeros.
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>(); // Diccionario para manejar a los pasajeros y sus controladores.

    public override void Start()
    {
        base.Start();

        // Inicializar los puntos de referencia globales basados en la posici�n inicial de la plataforma.
        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }

    void Update()
    {
        UpdateRaycastOrigins(); // Actualizar los or�genes de los rayos de detecci�n de colisiones.

        Vector3 velocity = CalculatePlatformMovement(); // Calcular el movimiento de la plataforma.

        CalculatePassengerMovement(velocity); // Calcular c�mo deben moverse los pasajeros con la plataforma.

        MovePassengers(true); // Mover los pasajeros antes de mover la plataforma.
        transform.Translate(velocity); // Mover la plataforma.
        MovePassengers(false); // Mover los pasajeros despu�s de mover la plataforma.
    }

    // M�todo para suavizar el movimiento de la plataforma.
    float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    // M�todo para calcular el movimiento de la plataforma entre puntos de referencia.
    Vector3 CalculatePlatformMovement()
    {
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero; // Si a�n no es tiempo de moverse, la plataforma permanece quieta.
        }

        // Determinar los �ndices de los puntos de referencia entre los cuales la plataforma se est� moviendo.
        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);

        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;

            // Si la plataforma no es c�clica, invertir la direcci�n al llegar al �ltimo punto.
            if (!cyclic)
            {
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints);
                }
            }
            nextMoveTime = Time.time + waitTime; // Establecer el tiempo de espera antes del pr�ximo movimiento.
        }

        return newPos - transform.position; // Devolver la distancia a mover en este frame.
    }

    // M�todo para mover a los pasajeros de la plataforma.
    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovement)
        {
            if (!passengerDictionary.ContainsKey(passenger.transform))
            {
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
            }

            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    // M�todo para calcular el movimiento de los pasajeros en funci�n del movimiento de la plataforma.
    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // Plataforma movi�ndose verticalmente.
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true));
                    }
                }
            }
        }

        // Plataforma movi�ndose horizontalmente.
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
                    }
                }
            }
        }

        // Pasajeros sobre una plataforma que se mueve horizontalmente o hacia abajo.
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            }
        }
    }

    // Estructura para almacenar la informaci�n del movimiento de los pasajeros.
    struct PassengerMovement
    {
        public Transform transform; // Transform del pasajero.
        public Vector3 velocity; // Velocidad a aplicar al pasajero.
        public bool standingOnPlatform; // Indica si el pasajero est� sobre la plataforma.
        public bool moveBeforePlatform; // Indica si el pasajero se mueve antes de que se mueva la plataforma.

        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

    // M�todo para dibujar los puntos de referencia en el editor.
    void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = .3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypointPos = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }
}
