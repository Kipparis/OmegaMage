using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpiker : PT_MonoBehaviour, Enemy {
    [SerializeField]
    private float _touchDamage = 0.5f;
    public float touchDamage {
        get { return (_touchDamage); }
        set { _touchDamage = value; }
    }

    public string typeString {
        get { return (roomXMLString); }
        set { roomXMLString = value; }
    }
    public float speed = 5f;
    public string roomXMLString = "{";

    public bool _______________;

    public Vector3 moveDir;
    public Transform characterTrans;

    private void Awake() {
        characterTrans = transform.Find("CharacterTrans");
    }

    private void Start() {
        // Задаём направление движения в зависимости от буквы в XML
        switch (roomXMLString) {
            case "^":
                moveDir = Vector3.up;
                break;
            case "v":
                moveDir = Vector3.down;
                break;
            case "{":
                moveDir = Vector3.left;
                break;
            case "}":
                moveDir = Vector3.right;
                break;
        }
    }

    private void FixedUpdate() {    // Происходит примерно 50 раз в сек
        rigidbody.velocity = moveDir * speed;
    }

    public void Damage(float amt, ElementType eT, bool damageOverTime = false) {
        // EnemySpiker ничего не наносит урон, ведь он просто шикарен
    }

    private void OnTriggerEnter(Collider other) {
        // Проверяем была ли это стена
        GameObject go = Utils.FindTaggedParent(other.gameObject);
        if (go == null) return; // Нет ничего с тэгом, возвращаем собсна

        if (go.tag == "Ground") { // Земля матушка
            // Смотрим двигается ли объект в нужно направлении
            // Дот продукт хелпует нам в таком деле
            float dot = Vector3.Dot(moveDir, go.transform.position - pos);
            if (dot > 0) { // Если Spiker движется навстречу блоку который он бьёт
                moveDir *= -1;
            }
        }
    }
}
