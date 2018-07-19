using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementInventoryButton : MonoBehaviour {

    public ElementType type;

    private void Awake() {
        // Переводим первый символ этого ио в число
        int typeNum = int.Parse(gameObject.name[0].ToString());

        // Переводим число в тип для элемента этого типа
        type = (ElementType)typeNum;
    }

    private void OnMouseUpAsButton() {
        // Говорим магу добавить этот тип элемента
        Mage.S.SelectElement(type);
    }
}
