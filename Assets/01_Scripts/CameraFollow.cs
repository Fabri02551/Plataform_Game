using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Referencia al objeto a seguir
    public Controller2D target;

    // Ajustes de seguimiento
    public float verticalOffset;
    public float lookAheadDstX;
    public float lookSmoothTimeX;
    public float verticalSmoothTime;
    public Vector2 focusAreaSize;

    // Variables internas
    private FocusArea focusArea;

    private float currentLookAheadX;
    private float targetLookAheadX;
    private float lookAheadDirX;
    private float smoothLookVelocityX;
    private float smoothVelocityY;

    private bool lookAheadStopped;

    void Start()
    {
        // Inicializaci�n del �rea de enfoque de la c�mara
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
    }

    void LateUpdate()
    {
        // Actualizar la posici�n del �rea de enfoque en funci�n de los l�mites del objeto a seguir
        focusArea.Update(target.collider.bounds);

        // Determinar la posici�n de enfoque de la c�mara
        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        // Manejar el desplazamiento hacia adelante (look ahead) en el eje X
        if (focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);

            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0)
            {
                // Si el jugador est� movi�ndose en la misma direcci�n que el �rea de enfoque, ajustar la vista hacia adelante
                lookAheadStopped = false;
                targetLookAheadX = lookAheadDirX * lookAheadDstX;
            }
            else
            {
                // Si el jugador se detiene o cambia de direcci�n, detener el desplazamiento hacia adelante suavemente
                if (!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDstX - currentLookAheadX) / 4f;
                }
            }
        }

        // Suavizar el desplazamiento hacia adelante en el eje X
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

        // Suavizar el desplazamiento vertical
        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);

        // Actualizar la posici�n de la c�mara
        focusPosition += Vector2.right * currentLookAheadX;
        transform.position = (Vector3)focusPosition + Vector3.forward * -10;
    }

    // M�todo para dibujar el �rea de enfoque en la vista de escena (para depuraci�n)
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.centre, focusAreaSize);
    }

    // Estructura que define el �rea de enfoque
    struct FocusArea
    {
        public Vector2 centre;
        public Vector2 velocity;

        private float left, right;
        private float top, bottom;

        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
        }

        // Actualiza el �rea de enfoque en funci�n del movimiento del objeto seguido
        public void Update(Bounds targetBounds)
        {
            float shiftX = 0;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }

            left += shiftX;
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top;
            }

            top += shiftY;
            bottom += shiftY;

            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(shiftX, shiftY);
        }
    }
}
