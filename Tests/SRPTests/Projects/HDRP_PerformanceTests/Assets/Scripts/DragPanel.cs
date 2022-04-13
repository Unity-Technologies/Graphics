using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class DragPanel : MonoBehaviour, IPointerDownHandler, IDragHandler {
	
	private Vector2 originalLocalPointerPosition;
	private Vector3 originalPanelLocalPosition;
	private RectTransform panelRectTransform;
	private RectTransform parentRectTransform;
	
	void Awake () {
		panelRectTransform = transform.parent as RectTransform;
		parentRectTransform = panelRectTransform.parent as RectTransform;
	}
	
	public void OnPointerDown (PointerEventData data) {
		originalPanelLocalPosition = panelRectTransform.localPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle (parentRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);
	}
	
	public void OnDrag (PointerEventData data) {
		if (panelRectTransform == null || parentRectTransform == null)
			return;
		
		Vector2 localPointerPosition;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle (parentRectTransform, data.position, data.pressEventCamera, out localPointerPosition)) {
			Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
			panelRectTransform.localPosition = originalPanelLocalPosition + offsetToOriginal;
		}
		
		ClampToWindow ();
	}
	
	// Clamp panel to area of parent
	void ClampToWindow () {
		Vector3 pos = panelRectTransform.localPosition;
		
		Vector3 minPosition = parentRectTransform.rect.min - panelRectTransform.rect.min;
		Vector3 maxPosition = parentRectTransform.rect.max - panelRectTransform.rect.max;
		
		pos.x = Mathf.Clamp (panelRectTransform.localPosition.x, minPosition.x, maxPosition.x);
		pos.y = Mathf.Clamp (panelRectTransform.localPosition.y, minPosition.y, maxPosition.y);
		
		panelRectTransform.localPosition = pos;
	}
}
