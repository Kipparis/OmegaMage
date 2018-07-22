using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGroundSpell : PT_MonoBehaviour {

    public float duration = 4;  // Жизнь этого ио
    public float durationVariance = 0.5f;
    // ^ позволяет времни варьироваться от 3.5 до 4.5
    public float fadeTime = 1f; // Время угасания
    public float timeStart; // Время рождения

    public float damagePerSecond = 5;

    private void Start() {
        timeStart = Time.time;
        duration = Random.Range(duration - durationVariance, duration + durationVariance);

    }

    private void Update() {
        float u = (Time.time - timeStart) / duration;

        // На каком у огонь должен угасать
        float fadePercent = 1 - (fadeTime / duration);
        if (u > fadePercent) { // Время угасания
            // тогда опускаем его в пол
            float u2 = (u - fadePercent) / (1 - fadePercent);
            // ^ u2 число между 0-1 просто для угасания
            Vector3 loc = pos;
            loc.z = u2 * 2; // Более низко со временем
            pos = loc;
        }

        if (u > 1) { // Если оно живёт больше чем длительность
            Destroy(gameObject);    // Уничтожаем его
        }
    }

    private void OnTriggerEnter(Collider other) {
        // Произносим что другой объект был затригерен
        GameObject go = Utils.FindTaggedParent(other.gameObject);
        if (go == null) {
            go = other.gameObject;
        }
        Utils.tr("Flame hit", go.name);
    }


    private void OnTriggerStay(Collider other) {
        // Дамажим
        EnemyBug recipient = other.GetComponent<EnemyBug>();
        // Если есть такой скрипт, наносим урон
        if (recipient != null) {
            recipient.Damage(damagePerSecond, ElementType.fire, true);
        }
    }
    //TODO: наносим урон другим объектам
}
