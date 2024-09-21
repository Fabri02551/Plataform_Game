using UnityEngine;
using System.Collections;

// Clase que controla el movimiento de un objeto en 2D, incluyendo detecci�n de colisiones y manejo de pendientes.
public class Controller2D : RaycastController
{
    // �ngulo m�ximo de pendiente que se puede subir.
    public float maxSlopeAngle = 80;

    // Informaci�n sobre las colisiones detectadas.
    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    // M�todo de inicializaci�n, que se ejecuta al comenzar el juego.
    public override void Start()
    {
        base.Start();
        // Inicializa la direcci�n en la que el objeto est� mirando (1 significa derecha).
        collisions.faceDir = 1;
    }

    // M�todo para mover el objeto sin considerar la entrada del jugador.
    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    // M�todo principal para mover el objeto, considerando la entrada del jugador y si est� sobre una plataforma.
    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        // Actualiza los or�genes de los rayos usados para la detecci�n de colisiones.
        UpdateRaycastOrigins();

        // Reinicia la informaci�n de colisiones.
        collisions.Reset();
        collisions.moveAmountOld = moveAmount;
        playerInput = input;

        // Si el objeto se est� moviendo hacia abajo, intenta descender por una pendiente.
        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }

        // Si el objeto se est� moviendo horizontalmente, actualiza la direcci�n en la que est� mirando.
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

        // Si est� parado en una plataforma, marca que est� por debajo.
        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    // M�todo para detectar y manejar colisiones horizontales.
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        // Asegura que los rayos siempre tengan un m�nimo de longitud.
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

            // Si el rayo detecta una colisi�n.
            if (hit)
            {
                // Si la distancia al objeto es 0, contin�a sin hacer nada.
                if (hit.distance == 0)
                {
                    continue;
                }

                // Calcula el �ngulo de la pendiente.
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // Si es el primer rayo y la pendiente es menor o igual al �ngulo m�ximo permitido.
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    // Si estaba descendiendo una pendiente, cancela esa acci�n.
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }
                    float distanceToSlopeStart = 0;

                    // Si la pendiente es diferente a la anterior, ajusta la posici�n del objeto.
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }

                    // Comienza a escalar la pendiente.
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                // Si no se est� escalando una pendiente o la pendiente es demasiado empinada.
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    // Ajusta la posici�n horizontal del objeto para evitar atravesar la colisi�n.
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    // Si se est� escalando una pendiente, ajusta la posici�n vertical.
                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    // Actualiza la informaci�n de colisiones horizontales.
                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    // M�todo para detectar y manejar colisiones verticales.
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

            // Si el rayo detecta una colisi�n.
            if (hit)
            {
                // Si la colisi�n es con una plataforma que se puede atravesar.
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

                // Ajusta la posici�n vertical del objeto para evitar atravesar la colisi�n.
                moveAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                // Si se est� escalando una pendiente, ajusta la posici�n horizontal.
                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                // Actualiza la informaci�n de colisiones verticales.
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        // Si se est� escalando una pendiente, verifica si hay una nueva pendiente al cambiar de direcci�n.
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

    // M�todo para escalar una pendiente.
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        // Si la altura necesaria para escalar la pendiente es mayor a la actual, ajusta la posici�n del objeto.
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

    // M�todo para descender una pendiente.
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

        // Si no se est� deslizando, verifica si se puede descender una pendiente menos empinada.
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

    // M�todo para deslizarse por una pendiente demasiado empinada.
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

    // M�todo para reiniciar el estado de "cayendo a trav�s de una plataforma".
    void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatform = false;
    }

    // Estructura que almacena la informaci�n de las colisiones.
    public struct CollisionInfo
    {
        public bool above, below; // Colisiones verticales.
        public bool left, right; // Colisiones horizontales.

        public bool climbingSlope; // Indica si el objeto est� escalando una pendiente.
        public bool descendingSlope; // Indica si el objeto est� descendiendo una pendiente.
        public bool slidingDownMaxSlope; // Indica si el objeto est� desliz�ndose por una pendiente empinada.

        public float slopeAngle, slopeAngleOld; // �ngulo de la pendiente actual y el anterior.
        public Vector2 slopeNormal; // Normal de la pendiente.
        public Vector2 moveAmountOld; // Movimiento anterior del objeto.
        public int faceDir; // Direcci�n en la que el objeto est� mirando.
        public bool fallingThroughPlatform; // Indica si el objeto est� cayendo a trav�s de una plataforma.

        // M�todo para reiniciar todos los estados de colisi�n.
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
