using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    // Parámetros de movimiento y salto
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    private float accelerationTimeAirborne = .2f;
    private float accelerationTimeGrounded = .1f;
    private float moveSpeed = 6;

    // Parámetros para saltos en pared
    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    // Parámetros para deslizarse en pared
    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f;
    private float timeToWallUnstick;

    // Variables internas de física
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector3 velocity;
    private float velocityXSmoothing;

    // Referencias
    private Controller2D controller;
    private Vector2 directionalInput;

    // Estado del jugador
    private bool wallSliding;
    private int wallDirX;

    void Start()
    {
        controller = GetComponent<Controller2D>();

        // Calcular la gravedad y las velocidades de salto
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update()
    {
        // Calcular la velocidad actual y manejar el deslizamiento en pared
        CalculateVelocity();
        HandleWallSliding();

        // Mover al jugador usando el controlador
        controller.Move(velocity * Time.deltaTime, directionalInput);

        // Resetear la velocidad en y cuando toca el suelo o techo
        if (controller.collisions.above || controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = 0;
            }
        }
    }

    // Método para establecer la entrada direccional
    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    // Método para manejar el salto al presionar el botón de salto
    public void OnJumpInputDown()
    {
        if (wallSliding)
        {
            // Saltar desde una pared
            if (wallDirX == directionalInput.x) // Saltar hacia la pared
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0) // Saltar lejos de la pared
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else // Saltar hacia la dirección opuesta
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }
        else if (controller.collisions.below)
        {
            // Saltar desde el suelo
            if (controller.collisions.slidingDownMaxSlope)
            {
                // Saltar mientras desciende una pendiente
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x)) // No saltar contra la pendiente
                {
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    // Método para reducir la altura del salto al soltar el botón
    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    // Método para manejar el deslizamiento en paredes
    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;

        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            // Limitar la velocidad de deslizamiento
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // Controlar el tiempo de "pegajosidad" en la pared
            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
                velocity.x = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }
    }

    // Método para calcular la velocidad en cada frame
    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }
}
