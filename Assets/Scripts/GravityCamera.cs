using UnityEngine;

public class GravityCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 defaultOffset = new Vector3(0, 3, -10);
    [SerializeField] float followSpeed = 5f;
    [SerializeField] float rotateSpeed = 5f;

    GravityCube cube;

    Quaternion targetRotation;    // целевая ориентация камеры
    Vector3 frozenOffset;         // оффсет, зафиксированный в момент прыжка
    bool wasGrounded;
    bool isOffsetFrozen = false; // Новый флаг: заморожен ли оффсет

    void Start()
    {
        if (target != null)
            cube = target.GetComponent<GravityCube>();

        targetRotation = transform.rotation;
        frozenOffset = defaultOffset;
    }

    void LateUpdate()
    {
        if (!target || cube == null) return;

        // Проверяем, была ли нажата клавиша смены гравитации в ЭТОМ кадре
        bool gravityKeyPressed = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) ||
                                 Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.F);

        // Момент отталкивания
        if (!cube.IsGrounded && wasGrounded)
        {
            // Сбрасываем флаг заморозки при каждом новом прыжке
            isOffsetFrozen = false;
        }
        // Если в полете и нажата клавиша - ЗАМОРАЖИВАЕМ оффсет
        else if (!cube.IsGrounded && gravityKeyPressed && !isOffsetFrozen)
        {
            frozenOffset = transform.position - target.position;
            isOffsetFrozen = true; // Фризим до следующего приземления
        }
        // Момент приземления
        else if (cube.IsGrounded && !wasGrounded)
        {
            targetRotation = Quaternion.LookRotation(target.forward, cube.SurfaceUp);
            // Размораживаем оффсет при приземлении
            isOffsetFrozen = false;
        }

        wasGrounded = cube.IsGrounded;

        // Логика расчета позиции
        Vector3 desiredPos;
        if (cube.IsGrounded)
        {
            // На земле: всегда обычный режим
            desiredPos = target.position + target.rotation * defaultOffset;
        }
        else
        {
            // В полете: если оффсет заморожен (была нажата клавиша) - используем frozenOffset
            if (isOffsetFrozen)
            {
                desiredPos = target.position + frozenOffset;
            }
            else
            {
                // Если не заморожен - продолжаем считать относительно текущей ориентации
                desiredPos = target.position + target.rotation * defaultOffset;
            }
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }
}
