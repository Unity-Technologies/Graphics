using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class SimpleBox : CanvasElement
	{
		protected string m_Title = "simpleBox";
		public SimpleBox(Vector2 position, float width)
		{
			translation = position;
			scale = new Vector2(width, width);
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
			Color selectedColor = new Color(1.0f, 0.7f, 0.0f, 0.7f);
			EditorGUI.DrawRect(new Rect(0, 0, scale.x, scale.y), selected ? selectedColor : backgroundColor );
			GUI.Label(new Rect(0, 0, scale.x, 26f), GUIContent.none, new GUIStyle("preToolbar"));
			GUI.Label(new Rect(10, 2, scale.x - 20.0f, 16.0f), m_Title, EditorStyles.toolbarTextField);
			base.Render(parentRect, canvas);
		}
	}

    public class MoveableBox : SimpleBox
	{
		public MoveableBox(Vector2 position, float width)
			: base(position,width)
		{
			m_Title = "Drag me!";
			AddManipulator(new Draggable());
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			base.Render(parentRect, canvas);
			
		}
	}

	class ResizableBox : SimpleBox
	{
		public ResizableBox(Vector2 position, float width)
			: base(position, width)
		{
			m_Title = "Resize me!";
			AddManipulator(new Resizable());
			AddManipulator(new Draggable());
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			base.Render(parentRect, canvas);
		}
	}

	class WWWImageBox : SimpleBox
	{
		Texture2D m_WWWTexture = new Texture2D(4, 4, TextureFormat.DXT1, false);
		WWW www = null;
		private float timeToNextPicture = 0.0f;

		public WWWImageBox(Vector2 position, float width)
			: base(position, width)
		{
			m_Title = "I cause repaints every frame!";
			AddManipulator(new Draggable());
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			if (www != null && www.isDone)
			{
				www.LoadImageIntoTexture(m_WWWTexture);
				www = null;
				timeToNextPicture = 3.0f;
			}

			timeToNextPicture -= Time.deltaTime;
			if (timeToNextPicture < 0.0f)
			{
				timeToNextPicture = 99999.0f;
				www = new WWW("http://lorempixel.com/200/200");
			}

			base.Render(parentRect, canvas);

			GUI.DrawTexture(new Rect(0, 20, 200, 200), m_WWWTexture);
			Invalidate();
			canvas.Repaint();
		}
	}

	class IMGUIControls : SimpleBox
	{
		private string m_Text1 = "this is a text field";
		private string m_Text2 = "this is a text field";
		private bool m_Toggle = true;
		private Texture2D m_aTexture = null;

		public IMGUIControls(Vector2 position, float width)
			: base(position, width)
		{
			m_Caps = Capabilities.Unselectable;

			m_Title = "modal";
			AddManipulator(new Draggable());
			AddManipulator(new Resizable());
			AddManipulator(new IMGUIContainer());
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			base.Render(parentRect, canvas);

			int currentY = 22;
			
			m_Text1 = GUI.TextField(new Rect(0, currentY, 80, 20), m_Text1);
			currentY += 22;
			
			m_Toggle = GUI.Toggle(new Rect(0, currentY, 10, 10), m_Toggle, GUIContent.none);
			currentY += 22;

			m_Text2 = GUI.TextField(new Rect(0, currentY, 80, 20), m_Text2);
			currentY += 22;

			m_aTexture = EditorGUI.ObjectField(new Rect(0, currentY, 80, 100), m_aTexture, typeof(Texture2D), false) as Texture2D;

		}
	}
}
