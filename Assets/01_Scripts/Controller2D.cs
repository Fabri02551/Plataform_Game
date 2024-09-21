using UnityEngine;
using System.Collections;

// Clase que controla el movimiento de un objeto en 2D, incluyendo detección de colisiones y manejo de pendientes.
public class Controller2D : RaycastController
{
    // Ángulo máximo de pendiente que se puede subir.
    public float maxSlopeAngle = 80;

    // Información sobre las colisiones detectadas.
    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    // Método de inicialización, que se ejecuta al comenzar el juego.
    public override void Start()
    {
        base.Start();
        // Inicializa la dirección en la que el objeto está mirando (1 significa derecha).
        collisions.faceDir = 1;
    }

    // Método para mover el objeto sin considerar la entrada del jugador.
    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    // Método principal para mover el objeto, considerando la entrada del jugador y si está sobre una plataforma.
    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        // Actualiza los orígenes de los rayos usados para la detección de colisiones.
        UpdateRaycastOrigins();

        // Reinicia la información de colisiones.
        collisions.Reset();
        collisions.moveAmountOld = moveAmount;
        playerInput = input;

        // Si el objeto se está moviendo hacia abajo, intenta descender por una pendiente.
        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }

        // Si el objeto se está moviendo horizontalmente, actualiza la dirección en la que está mirando.
        if (moveAmount.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        // Detecta colisiones horizontales.
        HorizontalCollisions(ref moveAmount);

        // Detecta colisiones verticales.
        if (moveAmount.y != 0)
        {
            VerticalCollisions(ref moveAmount);
        }

        // Mueve el objeto.
        transform.Translate(moveAmount);

        // Si está parado en una plataforma, marca que está por debajo.
        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    // Método para detectar y manejar colisiones horizontales.
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        // Asegura que los rayos siempre tengan un mínimo de longitud.
        if (Mathf.Abs(moveAmount.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        // Genera rayos para detectar colisiones horizontales.
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            // Si el rayo detecta una colisión.
            if (hit)
            {
                // Si la distancia al objeto es 0, continúa sin hacer nada.
                if (hit.distance == 0)
                {
                    continue;
                }

                // Calcula el ángulo de la pendiente.
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // Si es el primer rayo y la pendiente es menor o igual al ángulo máximo permitido.
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    // Si estaba descendiendo una pendiente, cancela esa acción.
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }
                    float distanceToSlopeStart = 0;

                    // Si la pendiente es diferente a la anterior, ajusta la posición del objeto.
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }

                    // Comienza a escalar la pendiente.
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                // Si no se está escalando una pendiente o la pendiente es demasiado empinada.
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    // Ajusta la posición horizontal del objeto para evitar atravesar la colisión.
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    // Si se está escalando una pendiente, ajusta la posición vertical.
                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    // Actualiza la información de colisiones horizontales.
                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    // Método para detectar y manejar colisiones verticales.
    void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        // Genera rayos para detectar colisiones verticales.
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

            // Si el rayo detecta una colisión.
            if (hit)
            {
                // Si la colisión es con una plataforma que se puede atravesar.
                if (hit.collider.CompareTag("Through"))
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if (collisions.fallingThroughPlatform)
                    {
                        continue;
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke(nameof(ResetFallingThroughPlatform), .5f);
                        continue;
                    }
                }

                // Ajusta la posición vertical del objeto para evitar atravesar la colisión.
                moveAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                // Si se está escalando una pendiente, ajusta la posición horizontal.
                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                // Actualiza la información de colisiones verticales.
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        // Si se está escalando una pendiente, verifica si hay una nueva pendiente al cambiar de dirección.
        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    // Método para escalar una pendiente.
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        // Si la altura necesaria para escalar la pendiente es mayor a la actual, ajusta la posición del objeto.
        if (moveAmount.y <= climbMoveAmountY)
        {
            moveAmount.y = climbMoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
    }

    // Método para descender una pendiente.
    void DescendSlope(ref Vector2 moveAmount)
    {
        // Verifica si hay una pendiente empinada en cualquiera de los lados del objeto.
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);

        // Si se detecta una pendiente empinada en uno de los lados, comienza a deslizarse.
        if (maxSlopeHitLeft ^ maxSlopeHitRight)
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }

        // Si no se está deslizando, verifica si se puede descender una pendiente menos empinada.
        if (!collisions.slidingDownMaxSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == directionX)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                        {
                            float moveDistance = Mathf.Abs(moveAmount.x);
                            float descendMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= descendMoveAmountY;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    // Método para deslizarse por una pendiente demasiado empinada.
    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);
                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    // Método para reiniciar el estado de "cayendo a través de una plataforma".
    void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatform = false;
    }

    // Estructura que almacena la información de las colisiones.
    public struct CollisionInfo
    {
        public bool above, below; // Colisiones verticales.
        public bool left, right; // Colisiones horizontales.

        public bool climbingSlope; // Indica si el objeto está escalando una pendiente.
        public bool descendingSlope; // Indica si el objeto está descendiendo una pendiente.
        public bool slidingDownMaxSlope; // Indica si el objeto está deslizándose por una pendiente empinada.

        public float slopeAngle, slopeAngleOld; // Ángulo de la pendiente actual y el anterior.
        public Vector2 slopeNormal; // Normal de la pendiente.
        public Vector2 moveAmountOld; // Movimiento anterior del objeto.
        public int faceDir; // Dirección en la que el objeto está mirando.
        public bool fallingThroughPlatform; // Indica si el objeto está cayendo a través de una plataforma.

        // Método para reiniciar todos los estados de colisión.
        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;
            slidingDownMaxSlope = false;

            slopeNormal = Vector2.zero;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
