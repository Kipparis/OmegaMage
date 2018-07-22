using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBug : PT_MonoBehaviour, Enemy {
    [SerializeField]
    private float _touchDamage = 1;
    public float touchDamage {
        get { return (_touchDamage); }
        set { _touchDamage = value; }
    }

    public string typeString {
        get { return (roomXMLString); }
        set { roomXMLString = value; }
    }

    public string roomXMLString;

    public float speed = 0.7f;

    public float health = 20;

    public float damageScale = 0.8f;
    public float damageScaleDuration = 0.25f;

    public bool _______________;

    private float damageScaleStartTime;

    private float _maxHealth;

    public Vector3 walkTarget;
    public bool walking;
    public Transform characterTrans;

    // Записывает урон от каждого элемента в кадр
    public Dictionary<ElementType, float> damageDict;
    // ^ Словари не появляются в инспекторе

    private void Awake() {
        characterTrans = transform.Find("CharacterTrans");
        _maxHealth = health;    // Используем чтобы была возможность хила
        ResetDamageDict();  // ПЕР: Перезагружаем словарь урона
    }

    // Очищает значения в damageDict словаре
    void ResetDamageDict() {
        if (damageDict == null) {
            damageDict = new Dictionary<ElementType, float>();
        }
        damageDict.Clear();
        damageDict.Add(ElementType.earth, 0);
        damageDict.Add(ElementType.water, 0);
        damageDict.Add(ElementType.air, 0);
        damageDict.Add(ElementType.fire, 0);
        damageDict.Add(ElementType.aether, 0);
        damageDict.Add(ElementType.none, 0);
    }

    private void Update() {
        WalkTo(Mage.S.pos);
    }

    //--------------- Код для хождения ------------
    // Весь код скопирован из Мага
    // Идём к определённой позиции. Координата z всегда 0
    public void WalkTo(Vector3 xTarget) {
        walkTarget = xTarget;   // Задаём позицию куда гуляем
        walkTarget.z = 0;   // z = 0
        walking = true; // Сейчас маг гуляет
        Face(walkTarget);    // Смотрим в направлении walkTarget
    }

    public void Face(Vector3 poi) { // Разворачиваемся к точке интереса
        Vector3 delta = poi - pos;
        // Используем Atan2 чтобы получить поворот вокруг Z который указывает на х осьот _Mage:CharacterTrans
        float rZ = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
        // Задаём поворот characterTrans ( на самом деле не поворачивает _Mage )
        characterTrans.rotation = Quaternion.Euler(0, 0, rZ);
    }

    public void StopWalking() { // Останавливает мага от хождения
        walking = false;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    void FixedUpdate() {    // Случается каждый шаг когда считается физика ( 50 /с-1 )
        if (walking) { // Если маг гуляет
            if ((walkTarget - pos).magnitude < Time.fixedDeltaTime * speed) {
                // Если маг очень близок к цели, просто останавливаемся
                pos = walkTarget;
                StopWalking();
            } else {
                // Движемся навстречу walkTarget
                GetComponent<Rigidbody>().velocity = (walkTarget - pos).normalized * speed;
            }
        } else {
            // Если не волкин, скорость равна нулю
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }

    // Калечим эту фигню. По дефолту урон мгновенный
    // но также можно сделать урон по времени
    // ЗАМЕТКА: Такой же код используется чтобы хилять
    public void Damage(float amt, ElementType eT, bool damageOverTime = false) {
        // Если это наносим урон в зависимости от времени
        if (damageOverTime) {
            amt *= Time.deltaTime;
        }

        // Обрабатываем разный урон по разному ( большинство рассматривается по дефолту )
        switch (eT) {
            case ElementType.earth:
                break;
            case ElementType.water:
                break;
            case ElementType.air:
                // Воздух не наносит урона жукам, так что ничего не делаем
                break;
            case ElementType.fire:
                // Только максимальный урон из источника засчитывается
                damageDict[eT] = Mathf.Max(amt, damageDict[eT]);
                break;
            case ElementType.aether:
                break;
            case ElementType.none:
                break;
            default:
                break;
        }
    }

    private void LateUpdate() {
        // Принимаем урон от разных элементов
        float dmg = 0;
        foreach (KeyValuePair<ElementType ,float> entry in damageDict) {
            dmg += entry.Value;
        }

        if (dmg > 0) {  // Если он получил урон
            // Если он вполный размер ( т.е. не получает урона )
            if (characterTrans.localScale == Vector3.one) {
                // Начинаем анимацию урона
                damageScaleStartTime = Time.time;
            }
        }

        // Анимация урона
        float damU = (Time.time - damageScaleStartTime) / damageScaleDuration;
        damU = Mathf.Min(1, damU);  // Ограничиваем максимальный размер
        float scl = (1 - damU) * damageScale + damU * 1;
        characterTrans.localScale = scl * Vector3.one;

        health -= dmg;
        health = Mathf.Min(_maxHealth, health); // Ограничиваем хп для лечения

        ResetDamageDict();  // Подготовка к следующему кадру

        if (health <= 0) {
            Die();
        }
    }

    // Делаем смерть отдельной функцией чтобы добавить в неё всякие плюхи для персонажа
    public void Die() {
        Destroy(gameObject);
    }
}
