using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /*
     TapIndicator использует PT_Mover класс из прототулс. Это позволяет использовать бизеровую
     кривую для изменения позиции, поворота, размера и т.д.

    Мы заметим что это добавит несколько публичных полей в инспектор
     */

public class TapIndicator : PT_Mover {

    public float lifeTime = 0.4f;   // Как долго будет продолжаться
    public float[] scales;  // Размеры для переходов
    public Color[] colors;  // Цвета для переходов

    void Awake() {
        scale = Vector3.zero;   // Это спрячет индикатор с самого начала
    }
     
    void Start() {
        // ПТ_Мувер основан на ПТ_Лок, который содержит инфу и данных позиции

        PT_Loc pLoc;
        List<PT_Loc> locs = new List<PT_Loc>();

        // Позиция всегда одинакова и всегда z = -0.1f
        Vector3 tPos = pos;
        tPos.z = -0.1f;

        // У нас должно быть одинаковое число размеров и цветов в инспекторе
        for (int i = 0; i < scales.Length; i++) {
            pLoc = new PT_Loc();
            pLoc.scale = Vector3.one * scales[i];   // Каждый размер
            pLoc.pos = tPos;
            pLoc.color = colors[i]; // и каждый цвет

            locs.Add(pLoc);
        }

        // callback это делегативная функция которая вызывает функцию() когда
        // движение закончится
        callback = CallbackMethod;  // Вызываем CallbackMethod когда закончим

        // Создаём движения передавая серию PT_Locs и продолжительность для кривой
        PT_StartMove(locs, lifeTime);
    }

    void CallbackMethod() {
        Destroy(gameObject);
    }
}
