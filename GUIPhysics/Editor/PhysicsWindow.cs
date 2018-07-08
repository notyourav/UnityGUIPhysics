using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
public class GUIPhysicsWindow : EditorWindow {
    static float gravity = 9.81F;
    static bool showFPS = true;
    static float bounciness = 0.25F;
    //Be careful with this:
    static float maxCollisionResolution = 0.01F;


    //GUI settings
    bool spawnWithMouse = true;
    GUIType guiType = GUIType.rigidbody;
    Vector2 controlSize = new Vector2(20, 20);

    //Used for FPS
    float lastTime;
    int updates;
    int totalFrameUpdates;

    static List<GUICollider> PhysicalGUIs = new List<GUICollider>();

    [MenuItem("Window/GUI Physics Test")]
    public static void CreateWindow() {
        GUIPhysicsWindow window = GetWindow<GUIPhysicsWindow>();
        window.maximized = true;
    }

    private void Update()
    {
        Repaint();
    }

    private void OnGUI()
    {
        PhysicalGUIs.ForEach((obj) =>
        {
            GUI.color = obj.color;
            if (obj is PGUI) {
                ((PGUI)obj).PhysicsUpdate();
            }
            GUI.Button(obj.rect, "");
            GUI.color = Color.white;
        });
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        spawnWithMouse = GUILayout.Toggle(spawnWithMouse, "Spawn With Mouse");
        guiType = (GUIType)EditorGUILayout.EnumPopup("Type", (System.Enum)guiType);
        controlSize = EditorGUILayout.Vector2Field("Size", controlSize);
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if (spawnWithMouse && Event.current.type == EventType.MouseDown)
        {
            switch (guiType)
            {
                case GUIType.rigidbody:
                    PhysicalGUIs.Add(new PGUI(Event.current.mousePosition, controlSize));
                    break;
                case GUIType.staticGUI:
                    PhysicalGUIs.Add(new GUICollider(Event.current.mousePosition, controlSize));
                    break;
            }
        }

        if (showFPS) {
            if (lastTime + 1 < Time.realtimeSinceStartup) {
                totalFrameUpdates = updates;
                updates = 0;
                lastTime = Time.realtimeSinceStartup;
            }
            updates += 1;
            GUI.Label(new Rect { position = new Vector2(position.width - 40, position.height - 20), size = new Vector2(40, 20) }, totalFrameUpdates.ToString());
        }
    }

    //We could use gameobjects as a model for physics but we don't want to clutter the users hierarchy.
    class GUICollider {
        public Rect rect;
        public Color color = Color.white;

        public GUICollider(Vector2 startPos, Vector2 size)
        {
            //Adds this instance to our own hierarchy of sorts.
            rect.position = startPos;
            rect.size = size;
        }
    }
    class PGUI : GUICollider {
        Vector2 velocity;

        public PGUI(Vector2 startPos, Vector2 size) : base(startPos, size)
        {

        }

        public void PhysicsUpdate() {
            VelocityUpdate();
            Move();

        }

        void VelocityUpdate() {
            velocity.y += gravity / 30000;
            velocity.y = Mathf.Clamp(velocity.y, -0.2F, 0.2F);
        }

        //Uses crappy AABB detection and resolves with completely wrong penetration depths
        void Move()
        {
            // Check X

            rect.x += velocity.x;
            PhysicalGUIs.ForEach((obj) =>
            {
                if (rect.Overlaps(obj.rect) && obj != this)
                {
                    float depth = GetXDepth(rect, obj.rect);
                    rect.x -= depth;
                    velocity.x *= -bounciness;
                    //velocity.x = 0;
                }
            });

           

            // Now check Y
            rect.y += velocity.y;
            PhysicalGUIs.ForEach((obj) =>
            {
                if (rect.Overlaps(obj.rect) && obj != this)
                {
                    float depth = GetYDepth(obj.rect, rect);
                    rect.y -= depth;
                    velocity.y *= -bounciness;
                    //velocity.y = 0;

                }
            });
        }

        float GetXDepth(Rect rect1, Rect rect2)
        {
            List<float> depths = new List<float>();
            float minDepth = Mathf.Infinity;
            depths.Add(rect1.x - rect2.x);
            depths.Add(rect1.x - rect2.x + rect2.width);
            depths.Add(rect1.x + rect1.width - rect2.x);
            depths.Add(rect1.x + rect1.width - rect2.x + rect2.width);
            depths.ForEach((obj) =>
            {
                if (Mathf.Abs(obj) < Mathf.Abs(minDepth))
                {
                    minDepth = obj;
                }
            });

            return (minDepth * maxCollisionResolution);
        }

        float GetYDepth(Rect rect1, Rect rect2)
        {
            List<float> depths = new List<float>();
            float minDepth = Mathf.Infinity;
            depths.Add(rect1.y - rect2.y);
            depths.Add(rect1.y - rect2.y + rect2.height);
            depths.Add(rect1.y + rect1.height - rect2.y);
            depths.Add(rect1.y + rect1.height - rect2.y + rect2.height);
            depths.ForEach((obj) =>
            {
                if (Mathf.Abs(obj) < Mathf.Abs(minDepth)) {
                    minDepth = obj;
                }
            });

            return (minDepth * maxCollisionResolution);
        }

    }

    enum GUIType {
        staticGUI,
        rigidbody,
    }
}
