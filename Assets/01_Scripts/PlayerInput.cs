using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour
{
    private Player player;

    void Start()
    {
        // Obtiene la referencia al componente Player al inicio
        player = GetComponent<Player>();
    }

    void Update()
    {
        // Obtener la entrada direccional del usuario (teclado o gamepad)
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.SetDirectionalInput(directionalInput);

        // Manejo del salto: al presionar la tecla de salto (espacio)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.OnJumpInputDown();
        }

        // Manejo del salto: al soltar la tecla de salto (espacio)
        if (Input.GetKeyUp(KeyCode.Space))
        {
            player.OnJumpInputUp();
        }
    }
}
