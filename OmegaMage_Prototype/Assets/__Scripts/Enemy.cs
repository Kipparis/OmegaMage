using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Enemy {
    // Объявление свойств которые будут введенны всеми классами
    // которые наследуют Enemy интерфейс
    Vector3 pos { get; set; }
    float touchDamage { get; set; }
    string typeString { get; set; } // Тип строчки из Room.xml

    // Следующее уже введенно во всех MonoBehaviour наследователях
    GameObject gameObject { get; }
    Transform transform { get; }
}