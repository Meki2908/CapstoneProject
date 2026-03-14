namespace ModularFootstepSystem.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using UnityEditorInternal;
    using ModularFootstepSystem.Extensions;

    /// <summary>
    /// Window for creating and editing a system of steps.
    /// </summary>
    public class ModularFootstepSystemEditor : EditorWindow
    {
        [SerializeField]
        protected GUISkin skin = default;

        protected int tab = 0;

        [MenuItem("Window/Modular Footstep System ^M")]
        public static void ShowWindow()
        {
            ModularFootstepSystemEditor window = GetWindow<ModularFootstepSystemEditor>("Modular Footstep System");
            window.minSize = new Vector2(300, 600);
        }

        protected virtual void OnEnable()
        {
            previousSelectedGameObject = null;
            showAnimatorNotExistHelpBox = false;

            if(selectedGameObject != null)
            {
                showAnimatorNotExistHelpBox = selectedGameObject.GetComponentInChildren<MeshFilter>() == null;
            }

            tab = 0;
            Repaint();
        }

        public void OnGUI()
        {
            switch (tab)
            {
                case 0:
                    FirstTab();
                    break;
                case 1:
                    SecondTab();
                    break;
                case 2:
                    ThirdTab();
                    ApplyEditSettersData();
                    break;
                case 3:
                    FourthTab();
                    break;
                default:
                    tab = 0;
                    FirstTab();
                    break;
            }
            
            ApplyFootstepControllerProperties();
            ApplyGroundDetectorProperties();
            ApplyFootHandlerProperties();
        }

        protected GameObject selectedGameObject = default;
        protected GameObject previousSelectedGameObject = default;
        protected FootstepsController footstepsController = default;
        protected FootstepsStateController footstepsStateController = default;
        protected GameObject footstepsControllerObject = default;
        protected Animator animator = default;
        protected FootstepsConductor conductor = default;

        protected string newFootHandlerName = string.Empty;

        protected Vector2 scrollPositionFirstTab = Vector2.zero;


        protected SerializedObject footstepsStateControllerObject = default;
        protected SerializedProperty defaultStateProperty = default;
        protected SerializedProperty initDefaultStateOnAwakeProperty = default;


        protected GroundDetectorUnderfoot editGroundDetector = default;
        protected SerializedObject groundDetectorObject = default;
        protected SerializedProperty footTransformProperty = default;
        protected SerializedProperty detectingDistanceToGroundProperty = default;
        protected SerializedProperty footPositionShiftProperty = default;
        protected SerializedProperty detectingLayersProperty = default;
        protected SerializedProperty drawGizmosProperty = default;
        protected SerializedProperty footMarkSizeProperty = default;

        protected FootHandler editFootHandler = default;
        protected SerializedObject footHandlerObject = default;
        protected SerializedProperty footTypeProperty = default;

        protected AbstractFootstepEffectCreator footprintCreator = default;
        protected AbstractFootstepEffectCreator footstepSoundCreator = default;
        protected AbstractFootstepEffectCreator footstepParticleCreator = default;
        protected SerializedProperty footstepCreatorsListProperty = default;

        protected AbstractFootstepDataSetter footprintDataSetter = default;
        protected AbstractFootstepDataSetter footstepSoundDataSetter = default;
        protected AbstractFootstepDataSetter footstepParticlesDataSetter = default;
        protected SerializedProperty footstepDataSettersListProperty = default;

        protected AbstractFootstepsDataChanger footprintsDataChanger = default;
        protected AbstractFootstepsDataChanger footstepSoundsDataChanger = default;
        protected AbstractFootstepsDataChanger footstepEffectsDataChanger = default;
        protected AbstractFootstepsDataChanger currentDataChanger = default;
        protected SerializedObject dataChangerObject = default;
        protected SerializedProperty dataChangerListProperty = default;

        protected ExtendedBehaviourPool footprintPool = default;
        protected SerializedObject footprintPoolObject = default;
        protected SerializedProperty footprintPoolPrefabProperty = default;
        protected SerializedProperty footprintPoolDefaultCapacityProperty = default;
        protected SerializedProperty footprintPoolMaxSizeProperty = default;
        protected SerializedProperty footprintPoolIsReusedActiveObjectsProperty = default;


        protected SerializedObject footstepSoundCreatorObject = default;
        protected SerializedProperty footstepAudioSourceProperty = default;


        protected SerializedObject footstepEffectsDataObject = default;
        protected SerializedProperty effectsDefaultPoolCapacityProperty = default;
        protected SerializedProperty effectsMaxPoolSizeProperty = default;


        protected AbstractFootstepDataSetter editDataSetter = default;
        protected SerializedObject setterObject = default;
        protected SerializedProperty dataList = default;


        protected SerializedObject footstepControllerObject = default;
        protected SerializedProperty footstepStartHandlersProperty = default;
        protected SerializedProperty footstepMiddleHandlersProperty = default;
        protected SerializedProperty footstepEndHandlersProperty = default;


        protected ReorderableList footstepStartHandlersList = default;
        protected ReorderableList footstepMiddleHandlersList = default;
        protected ReorderableList footstepEndHandlersList = default;
        protected ReorderableList moduleData = default;

        protected bool showHandlerNameHelpBox = false;
        protected bool showAnimatorNotExistHelpBox = false;

        protected virtual ReorderableList InitializeHandlersList(SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            ReorderableList reorderableList = new ReorderableList(serializedObject, serializedProperty, true, true, true, true);

            reorderableList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x + 5, rect.y, rect.width - 95, EditorGUIUtility.singleLineHeight), element, GUIContent.none);

                if (GUI.Button(new Rect(rect.x + rect.width - 80, rect.y, 75, EditorGUIUtility.singleLineHeight), "Edit"))
                {
                    editFootHandler = reorderableList.serializedProperty.GetArrayElementAtIndex(reorderableList.index).boxedValue as FootHandler;

                    if (editFootHandler != null)
                    {
                        GetFootHandlerProperties();
                        GetGroundDetectorProperties();
                        tab = 1;
                    }
                }
            };

            reorderableList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, serializedProperty.displayName);

            reorderableList.onAddCallback =
            (ReorderableList list) =>
            {
                if(string.IsNullOrEmpty(newFootHandlerName) || string.IsNullOrWhiteSpace(newFootHandlerName))
                {
                    showHandlerNameHelpBox = true;
                    return;
                }

                serializedProperty.arraySize++;
                int newIndex = serializedProperty.arraySize - 1;

                GameObject footHandler = new GameObject(newFootHandlerName);
                footHandler.transform.parent = footstepsController.transform;
                list.serializedProperty.GetArrayElementAtIndex(newIndex).boxedValue = footHandler.AddComponent<FootHandler>();

                GUI.FocusControl(null);

                newFootHandlerName = string.Empty;
            };

            reorderableList.onRemoveCallback =
            (ReorderableList list) =>
            {
                FootHandler footHandler = list.serializedProperty.GetArrayElementAtIndex(list.index).boxedValue as FootHandler;
                if (footHandler != null)
                {
                    DestroyImmediate(footHandler.gameObject);
                }
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };

            return reorderableList;
        }

        protected virtual void FirstTab()
        {
            GUILayout.Label("Modular Footstem System", skin.customStyles[3]);

            scrollPositionFirstTab = GUILayout.BeginScrollView(scrollPositionFirstTab);

            GUILayout.Label("Select an object to initialize the step system");

            GUILayout.BeginHorizontal();

            selectedGameObject = EditorGUILayout.ObjectField(selectedGameObject, typeof(GameObject), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as GameObject;

            if (selectedGameObject != null && previousSelectedGameObject != selectedGameObject)
            {
                footstepsController = selectedGameObject.GetComponentInChildren<FootstepsController>();
                animator = selectedGameObject.GetComponentInChildren<Animator>();

                conductor = animator != null ? animator.gameObject.GetComponent<FootstepsConductor>() : null;

                previousSelectedGameObject = selectedGameObject;

                if (footstepsController != null)
                {
                    footstepControllerObject = new SerializedObject(footstepsController);
                    footstepStartHandlersProperty = footstepControllerObject.FindProperty("footstepStartHandlers");
                    footstepMiddleHandlersProperty = footstepControllerObject.FindProperty("footstepMiddleHandlers");
                    footstepEndHandlersProperty = footstepControllerObject.FindProperty("footstepEndHandlers");

                    footstepStartHandlersList = InitializeHandlersList(footstepControllerObject, footstepStartHandlersProperty);
                    footstepMiddleHandlersList = InitializeHandlersList(footstepControllerObject, footstepMiddleHandlersProperty);
                    footstepEndHandlersList = InitializeHandlersList(footstepControllerObject, footstepEndHandlersProperty);
                }

                footstepsStateController = selectedGameObject.GetComponentInChildren<FootstepsStateController>();

                if (footstepsStateController != null)
                {
                    footstepsStateControllerObject = new SerializedObject(footstepsStateController);
                    defaultStateProperty = footstepsStateControllerObject.FindProperty("defaultStateType");
                    initDefaultStateOnAwakeProperty = footstepsStateControllerObject.FindProperty("initDefaultStateOnAwake");
                }

                showHandlerNameHelpBox = false;
                showAnimatorNotExistHelpBox = false;
            }

            if(footstepsController == null && GUILayout.Button("Initialize system"))
            {
                footstepsController = selectedGameObject.GetComponentInChildren<FootstepsController>();
                animator = selectedGameObject.GetComponentInChildren<Animator>();

                if (footstepsController == null)
                {
                    if (animator == null)
                    {
                        showAnimatorNotExistHelpBox = true;
                    }
                    else
                    {
                        footstepsControllerObject = new GameObject("ModularFootstepSystem");
                        footstepsControllerObject.transform.parent = selectedGameObject.transform;
                        footstepsController = footstepsControllerObject.AddComponent<FootstepsController>();

                        footstepControllerObject = new SerializedObject(footstepsController);
                        footstepStartHandlersProperty = footstepControllerObject.FindProperty("footstepStartHandlers");
                        footstepMiddleHandlersProperty = footstepControllerObject.FindProperty("footstepMiddleHandlers");
                        footstepEndHandlersProperty = footstepControllerObject.FindProperty("footstepEndHandlers");

                        footstepStartHandlersList = InitializeHandlersList(footstepControllerObject, footstepStartHandlersProperty);
                        footstepMiddleHandlersList = InitializeHandlersList(footstepControllerObject, footstepMiddleHandlersProperty);
                        footstepEndHandlersList = InitializeHandlersList(footstepControllerObject, footstepEndHandlersProperty);

                        if(animator.gameObject.TryGetComponent(out conductor) == false)
                        {
                            conductor = animator.gameObject.AddComponent<FootstepsConductor>();
                        }
                        
                        footstepControllerObject.FindProperty("footstepsConductor").boxedValue = conductor;
                        footstepControllerObject.ApplyModifiedProperties();

                        showHandlerNameHelpBox = false;
                        showAnimatorNotExistHelpBox = false;
                    }
                }
            }

            if (footstepsController != null)
            {
                if (GUILayout.Button("Remove system"))
                {
                    if (EditorUtility.DisplayDialog("Warning", "Do you really want to remove system?", "Yes", "No"))
                    {
                        DestroyImmediate(footstepsController.gameObject);
                        footstepsController = null;

                        if (conductor != null)
                        {
                            DestroyImmediate(conductor);
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();

            if (showAnimatorNotExistHelpBox)
            {
                EditorGUILayout.HelpBox("Animator not found. Add animator to your entity", MessageType.Error);
            }

            if (selectedGameObject == null || footstepsController == null)
            {
                GUILayout.EndScrollView();
                return;
            }

            GUILayout.Space(10);


            GUILayout.Label("Enter the name of the foot and click «+» to add it");
            GUILayout.BeginHorizontal();
            GUILayout.Label("New foot name:", GUILayout.Width(100));
            newFootHandlerName = EditorGUILayout.TextField(newFootHandlerName);

            GUILayout.EndHorizontal();

            if(showHandlerNameHelpBox && !string.IsNullOrEmpty(newFootHandlerName) && !string.IsNullOrWhiteSpace(newFootHandlerName))
            {
                showHandlerNameHelpBox = false;
            }

            if (showHandlerNameHelpBox)
            {
                EditorGUILayout.HelpBox("Please, write foot name to field in upside!", MessageType.Warning);
            }

            GUILayout.BeginVertical(skin.box);
            if (footstepControllerObject != null && footstepControllerObject.targetObject != null)
            {
                footstepStartHandlersList.DoLayoutList();
                footstepMiddleHandlersList.DoLayoutList();
                footstepEndHandlersList.DoLayoutList();
            }
            GUILayout.EndVertical();


            if (footstepsController != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Footsteps state Controller", skin.customStyles[4]);
                GUILayout.BeginVertical(skin.box);
                if (footstepsStateController == null)
                {
                    EditorGUILayout.HelpBox("To configure the controller, add a data changer to any foot module.", MessageType.Info);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(defaultStateProperty.displayName, GUILayout.Width(165));
                    EditorGUILayout.PropertyField(defaultStateProperty, GUIContent.none);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(initDefaultStateOnAwakeProperty.displayName, GUILayout.Width(165));
                    EditorGUILayout.PropertyField(initDefaultStateOnAwakeProperty, GUIContent.none);
                    GUILayout.EndHorizontal();

                    if (footstepsStateControllerObject != null && footstepsStateControllerObject.targetObject != null && footstepsStateControllerObject.hasModifiedProperties)
                    {
                        footstepsStateControllerObject.ApplyModifiedProperties();
                    }
                }
                GUILayout.EndVertical();
            }


            GUILayout.EndScrollView();
        }

        protected virtual void GetFootHandlerProperties()
        {
            footHandlerObject = new SerializedObject(editFootHandler);

            footTypeProperty = footHandlerObject.FindProperty("footType");

            footstepCreatorsListProperty = footHandlerObject.FindProperty("footstepCreators");

            footprintCreator = default;
            footstepSoundCreator = default;
            footstepParticleCreator = default;

            for (int creatorNumber = 0; creatorNumber < footstepCreatorsListProperty.arraySize; creatorNumber++)
            {
                SerializedProperty footstepCreator = footstepCreatorsListProperty.GetArrayElementAtIndex(creatorNumber);

                if (footstepCreator.boxedValue is DecalFootprintsCreator)
                {
                    footprintCreator = footstepCreator.boxedValue as DecalFootprintsCreator;
                }

                if (footstepCreator.boxedValue is FootstepSoundCreator)
                {
                    footstepSoundCreator = footstepCreator.boxedValue as FootstepSoundCreator;
                }

                if (footstepCreator.boxedValue is FootstepParticleEffectCreator)
                {
                    footstepParticleCreator = footstepCreator.boxedValue as FootstepParticleEffectCreator;
                }
            }

            footstepDataSettersListProperty = footHandlerObject.FindProperty("footstepDataSetters");

            footprintDataSetter = default;
            footstepSoundDataSetter = default;
            footstepParticlesDataSetter = default;

            for (int setterNumber = 0; setterNumber < footstepDataSettersListProperty.arraySize; setterNumber++)
            {
                SerializedProperty footstepSetter = footstepDataSettersListProperty.GetArrayElementAtIndex(setterNumber);

                if (footstepSetter.boxedValue is FootprintDataSetter)
                {
                    footprintDataSetter = footstepSetter.boxedValue as FootprintDataSetter;
                }

                if (footstepSetter.boxedValue is FootstepSoundSetter)
                {
                    footstepSoundDataSetter = footstepSetter.boxedValue as FootstepSoundSetter;
                }

                if (footstepSetter.boxedValue is FootstepParticleEffectSetter)
                {
                    footstepParticlesDataSetter = footstepSetter.boxedValue as FootstepParticleEffectSetter;
                }
            }

            RemoveMissedModules();

            if (footprintCreator != null)
            {
                footprintPool = footprintCreator.GetComponent<ExtendedBehaviourPool>();

                if (footprintPool != null)
                {
                    footprintPoolObject = new SerializedObject(footprintPool);
                    footprintPoolPrefabProperty = footprintPoolObject.FindProperty("prefab");
                    footprintPoolDefaultCapacityProperty = footprintPoolObject.FindProperty("defaultCapacity");
                    footprintPoolMaxSizeProperty = footprintPoolObject.FindProperty("maxSize");
                    footprintPoolIsReusedActiveObjectsProperty = footprintPoolObject.FindProperty("isReusedActiveObjects");
                }
            }

            if(footstepSoundCreator != null)
            {
                footstepSoundCreatorObject = new SerializedObject(footstepSoundCreator);
                footstepAudioSourceProperty = footstepSoundCreatorObject.FindProperty("audioSource");
            }

            if(footstepParticlesDataSetter != null)
            {
                footstepEffectsDataObject = new SerializedObject(footstepParticlesDataSetter);
                effectsDefaultPoolCapacityProperty = footstepEffectsDataObject.FindProperty("effectsPoolDefaultCapacity");

                effectsMaxPoolSizeProperty = footstepEffectsDataObject.FindProperty("effectsPoolMaxCapacity");
            }

            footprintsDataChanger = default;
            footstepSoundsDataChanger = default;
            footstepEffectsDataChanger = default;

            if (footprintCreator != null)
            {
                footprintsDataChanger = footprintCreator.GetComponent<FootprintsChanger>();
            }

            if (footstepSoundCreator)
            {
                footstepSoundsDataChanger = footstepSoundCreator.GetComponent<FootstepSoundsChanger>();
            }

            if (footstepParticleCreator)
            {
                footstepEffectsDataChanger = footstepParticleCreator.GetComponent<FootstepEffectsChanger>();
            }
        }

        protected virtual void GetGroundDetectorProperties()
        {
            editGroundDetector = editFootHandler.GetComponent<GroundDetectorUnderfoot>();
            groundDetectorObject = new SerializedObject(editGroundDetector);
            footTransformProperty = groundDetectorObject.FindProperty("footTranform");
            detectingDistanceToGroundProperty = groundDetectorObject.FindProperty("detectingDistanceToGround");
            footPositionShiftProperty = groundDetectorObject.FindProperty("footPositionShift");
            detectingLayersProperty = groundDetectorObject.FindProperty("detectingLayers");
            drawGizmosProperty = groundDetectorObject.FindProperty("drawGizmos");
            footMarkSizeProperty = groundDetectorObject.FindProperty("footMarkSize");
        }

        protected Vector2 scrollPosition = Vector2.zero;

        protected virtual void SecondTab()
        {
            if(editFootHandler == null)
            {
                tab = 0;
                FirstTab();
                return;
            }

            GUILayout.Label("Modular Footstem System", skin.customStyles[3]);
            if (GUILayout.Button("Back", GUILayout.Width(60)))
            {
                showAnimatorNotExistHelpBox = false;
                tab = 0;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            GUILayout.Label($"Editable foot: {editFootHandler.gameObject.name}", skin.customStyles[4]);

            DrawGroundDetectorProperties();
            GUILayout.Space(20);
            DrawFootHandlerProperties();

            EditorGUILayout.EndScrollView();
        }

        protected virtual void DrawGroundDetectorProperties()
        {
            GUILayout.Label($"Ground detector properties", skin.customStyles[4]);
            GUILayout.BeginVertical(skin.box);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(footTransformProperty.displayName, GUILayout.Width(100));
            EditorGUILayout.PropertyField(footTransformProperty, GUIContent.none);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(detectingDistanceToGroundProperty.displayName, GUILayout.Width(175));
            EditorGUILayout.PropertyField(detectingDistanceToGroundProperty, GUIContent.none);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(footPositionShiftProperty.displayName, GUILayout.Width(175));
            EditorGUILayout.PropertyField(footPositionShiftProperty, GUIContent.none);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(detectingLayersProperty.displayName, GUILayout.Width(175));
            EditorGUILayout.PropertyField(detectingLayersProperty, GUIContent.none);
            GUILayout.EndHorizontal();
            
            
            GUILayout.Space(20);
            GUILayout.Label($"Gizmos settings", skin.customStyles[4]);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(drawGizmosProperty.displayName, GUILayout.Width(175));
            EditorGUILayout.PropertyField(drawGizmosProperty, GUIContent.none);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(footMarkSizeProperty.displayName, GUILayout.Width(175));
            EditorGUILayout.PropertyField(footMarkSizeProperty, GUIContent.none);
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }

        protected virtual void RemoveMissedModules()
        {
            List<int> removingElementsIndexes = new List<int>();
            for (int creatorNumber = 0; creatorNumber < footstepCreatorsListProperty.arraySize; creatorNumber++)
            {
                SerializedProperty footstepCreator = footstepCreatorsListProperty.GetArrayElementAtIndex(creatorNumber);
                if(footstepCreator.boxedValue == null)
                {
                    removingElementsIndexes.Add(creatorNumber);
                }
            }
            for(int removingElementsIndexesNumber = removingElementsIndexes.Count - 1; removingElementsIndexesNumber >= 0; removingElementsIndexesNumber--)
            {
                footstepCreatorsListProperty.DeleteArrayElementAtIndex(removingElementsIndexes[removingElementsIndexesNumber]);
            }

            removingElementsIndexes.Clear();

            for (int setterNumber = 0; setterNumber < footstepDataSettersListProperty.arraySize; setterNumber++)
            {
                SerializedProperty footstepSetter = footstepDataSettersListProperty.GetArrayElementAtIndex(setterNumber);
                if (footstepSetter.boxedValue == null)
                {
                    removingElementsIndexes.Add(setterNumber);
                }
            }
            for (int removingElementsIndexesNumber = removingElementsIndexes.Count - 1; removingElementsIndexesNumber >= 0; removingElementsIndexesNumber--)
            {
                footstepDataSettersListProperty.DeleteArrayElementAtIndex(removingElementsIndexes[removingElementsIndexesNumber]);
            }

            footHandlerObject.ApplyModifiedProperties();
        }

        protected virtual void RemoveModule(AbstractFootstepEffectCreator creator, AbstractFootstepDataSetter setter)
        {
            int creatorIndex = -1;
            for (int creatorNumber = 0; creatorNumber < footstepCreatorsListProperty.arraySize; creatorNumber++)
            {
                SerializedProperty footstepCreator = footstepCreatorsListProperty.GetArrayElementAtIndex(creatorNumber);

                if ((footstepCreator.boxedValue is DecalFootprintsCreator && creator is DecalFootprintsCreator) ||
                    (footstepCreator.boxedValue is FootstepSoundCreator && creator is FootstepSoundCreator) ||
                    (footstepCreator.boxedValue is FootstepParticleEffectCreator && creator is FootstepParticleEffectCreator))
                {
                    creatorIndex = creatorNumber;
                    break;
                } 
            }

            if (creatorIndex >= 0)
            {
                footstepCreatorsListProperty.DeleteArrayElementAtIndex(creatorIndex);
            }

            int setterIndex = -1;
            for (int setterNumber = 0; setterNumber < footstepDataSettersListProperty.arraySize; setterNumber++)
            {
                SerializedProperty footstepSetter = footstepDataSettersListProperty.GetArrayElementAtIndex(setterNumber);

                if ((footstepSetter.boxedValue is FootprintDataSetter && setter is FootprintDataSetter) ||
                    (footstepSetter.boxedValue is FootstepSoundSetter && setter is FootstepSoundSetter) ||
                    (footstepSetter.boxedValue is FootstepParticleEffectSetter && setter is FootstepParticleEffectSetter))
                {
                    setterIndex = setterNumber;
                    break;
                }
            }

            if (setterIndex >= 0)
            {
                footstepDataSettersListProperty.DeleteArrayElementAtIndex(setterIndex);
            }

            footHandlerObject.ApplyModifiedProperties();
        }

        protected virtual void DrawFootHandlerProperties()
        {
            GUILayout.Label($"Foot Handler Properties", skin.customStyles[4]);
            GUILayout.BeginVertical(skin.box);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(footTypeProperty.displayName, GUILayout.Width(60));
            EditorGUILayout.PropertyField(footTypeProperty, GUIContent.none);
            if (GUILayout.Button("Create new", GUILayout.Width(85)))
            {
                FootType footType = CreateInstance<FootType>();
                string path = EditorUtility.SaveFilePanelInProject("Create new foot type", "New foot type", "asset", "asset");
                
                if(!string.IsNullOrEmpty(path) && !string.IsNullOrWhiteSpace(path))
                {
                    AssetDatabase.CreateAsset
                    (
                        footType,
                        path
                    );
                    footTypeProperty.boxedValue = footType;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label("Modules", skin.customStyles[4]);
            GUILayout.BeginVertical(skin.box);
            DrawFootprintModule();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical(skin.box);
            DrawFootstepSoundsModule();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical(skin.box);
            DrawFootstepEffectsModule();
            GUILayout.EndVertical();


            GUILayout.EndVertical();
        }

        protected virtual void DrawFootprintModule()
        {
            GUILayout.Label("Footprints", skin.customStyles[4]);
            
            if (footprintCreator == null)
            {
                if (GUILayout.Button("Add"))
                {
                    GameObject footprintObject = new GameObject("Footprint");
                    footprintObject.transform.parent = editFootHandler.transform;

                    footprintCreator = footprintObject.AddComponent<DecalFootprintsCreator>();
                    footprintDataSetter = footprintObject.AddComponent<FootprintDataSetter>();
                    footprintPool = footprintObject.AddComponent<ExtendedBehaviourPool>();

                    footprintPoolObject = new SerializedObject(footprintPool);
                    footprintPoolPrefabProperty = footprintPoolObject.FindProperty("prefab");
                    footprintPoolDefaultCapacityProperty = footprintPoolObject.FindProperty("defaultCapacity");
                    footprintPoolMaxSizeProperty = footprintPoolObject.FindProperty("maxSize");
                    footprintPoolIsReusedActiveObjectsProperty = footprintPoolObject.FindProperty("isReusedActiveObjects");

                    SerializedObject footprintCreatorObject = new SerializedObject(footprintCreator);
                    footprintCreatorObject.FindProperty("footprintPool").boxedValue = footprintPool;
                    footprintCreatorObject.ApplyModifiedProperties();

                    SerializedObject footprintDataSetterObject = new SerializedObject(footprintDataSetter);
                    footprintDataSetterObject.FindProperty("footprintsCreator").boxedValue = footprintCreator;
                    footprintDataSetterObject.ApplyModifiedProperties();

                    footstepCreatorsListProperty.InsertArrayElementAtIndex(0);
                    footstepCreatorsListProperty.GetArrayElementAtIndex(0).boxedValue = footprintCreator;

                    footstepDataSettersListProperty.InsertArrayElementAtIndex(0);
                    footstepDataSettersListProperty.GetArrayElementAtIndex(0).boxedValue = footprintDataSetter;

                    footHandlerObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (GUILayout.Button("Remove"))
                {
                    footprintPoolObject = null;
                    RemoveModule(footprintCreator, footprintDataSetter);
                    DestroyImmediate(footprintCreator.gameObject);
                }
            }

            if (footprintCreator != null && footprintPool != null)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(footprintPoolPrefabProperty.displayName, GUILayout.Width(50));
                EditorGUILayout.PropertyField(footprintPoolPrefabProperty, GUIContent.none);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(footprintPoolDefaultCapacityProperty.displayName, GUILayout.Width(120));
                EditorGUILayout.PropertyField(footprintPoolDefaultCapacityProperty, GUIContent.none);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(footprintPoolMaxSizeProperty.displayName, GUILayout.Width(120));
                EditorGUILayout.PropertyField(footprintPoolMaxSizeProperty, GUIContent.none);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(footprintPoolIsReusedActiveObjectsProperty.displayName, GUILayout.Width(143));
                EditorGUILayout.PropertyField(footprintPoolIsReusedActiveObjectsProperty, GUIContent.none);
                GUILayout.EndHorizontal();
            }

            if (footprintDataSetter != null)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Footprints data setter:", GUILayout.Width(180));
                if (GUILayout.Button("Edit", GUILayout.Width(55)))
                {
                    editDataSetter = footprintDataSetter;
                    GetEditSettersData(editDataSetter);
                    tab = 2;
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Footprints changer by action:", GUILayout.Width(180));
                if (footprintsDataChanger == null)
                {
                    if (GUILayout.Button("Add", GUILayout.Width(55)))
                    {
                        if (footstepsStateController == null)
                        {
                            footstepsStateController = footstepsController.gameObject.AddComponent<FootstepsStateController>();
                        }

                        footprintsDataChanger = footprintDataSetter.gameObject.AddComponent<FootprintsChanger>();
                        SerializedObject footprintDataChangerObject = new SerializedObject(footprintsDataChanger);
                        footprintDataChangerObject.FindProperty("dataSetter").boxedValue = footprintDataSetter;
                        footprintDataChangerObject.ApplyModifiedProperties();

                        footstepsStateControllerObject = new SerializedObject(footstepsStateController);
                        SerializedProperty footstepsStateControllerProperty = footstepsStateControllerObject.FindProperty("changers");
                        footstepsStateControllerProperty.InsertArrayElementAtIndex(0);
                        footstepsStateControllerProperty.GetArrayElementAtIndex(0).boxedValue = footprintsDataChanger;
                        defaultStateProperty = footstepsStateControllerObject.FindProperty("defaultStateType");
                        initDefaultStateOnAwakeProperty = footstepsStateControllerObject.FindProperty("initDefaultStateOnAwake");
                        footstepsStateControllerObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    if (GUILayout.Button("Edit", GUILayout.Width(55)))
                    {
                        currentDataChanger = footprintsDataChanger;
                        dataChangerObject = new SerializedObject(currentDataChanger);
                        dataChangerListProperty = dataChangerObject.FindProperty("stateData");
                        tab = 3;
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(65)))
                    {
                        RemoveChanger(footprintsDataChanger);

                        DestroyImmediate(footprintsDataChanger);
                        footprintsDataChanger = null;
                    }
                }
                GUILayout.EndHorizontal();
                                
                if (!DecalEnabler.IsDecalsEnabled())
                {
                    EditorGUILayout.Space(10);
                    EditorStyles.helpBox.fontSize += 2;
                    EditorGUILayout.HelpBox(
                        "To display footprints correctly, the Decal feature must be enabled. Enable this feature for the current Render Pipeline Asset?",
                        MessageType.Error);
                    EditorStyles.helpBox.fontSize -= 2;
                    
                    if (GUILayout.Button("Enable Decals", GUILayout.Width(100)))
                    {
                        DecalEnabler.EnableDecals();
                    }
                }


            }
        }

        protected virtual void DrawFootstepSoundsModule()
        {
            GUILayout.Label("Footstep sounds", skin.customStyles[4]);
            if (footstepSoundCreator == null)
            {
                if (GUILayout.Button("Add"))
                {
                    GameObject footstepSoundsObject = new GameObject("StepSounds");
                    footstepSoundsObject.transform.parent = editFootHandler.transform;

                    footstepSoundCreator = footstepSoundsObject.AddComponent<FootstepSoundCreator>();
                    footstepSoundDataSetter = footstepSoundsObject.AddComponent<FootstepSoundSetter>();

                    SerializedObject footstepAudioDataSetterObject = new SerializedObject(footstepSoundDataSetter);
                    footstepAudioDataSetterObject.FindProperty("footstepSoundCreator").boxedValue = footstepSoundCreator;
                    footstepAudioDataSetterObject.ApplyModifiedProperties();

                    footstepSoundCreatorObject = new SerializedObject(footstepSoundCreator);
                    footstepAudioSourceProperty = footstepSoundCreatorObject.FindProperty("audioSource");

                    footstepCreatorsListProperty.InsertArrayElementAtIndex(0);
                    footstepCreatorsListProperty.GetArrayElementAtIndex(0).boxedValue = footstepSoundCreator;

                    footstepDataSettersListProperty.InsertArrayElementAtIndex(0);
                    footstepDataSettersListProperty.GetArrayElementAtIndex(0).boxedValue = footstepSoundDataSetter;

                    footHandlerObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (GUILayout.Button("Remove"))
                {
                    RemoveModule(footstepSoundCreator, footstepSoundDataSetter);
                    DestroyImmediate(footstepSoundCreator.gameObject);
                }
            }


            if (footstepSoundCreator != null)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(footstepAudioSourceProperty.displayName, GUILayout.Width(90));
                EditorGUILayout.PropertyField(footstepAudioSourceProperty, GUIContent.none);
                GUILayout.EndHorizontal();
            }

            if (footstepSoundDataSetter != null)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                GUILayout.Label("Footstep sound data setter:", GUILayout.Width(180));
                if (GUILayout.Button("Edit", GUILayout.Width(55)))
                {
                    editDataSetter = footstepSoundDataSetter;
                    GetEditSettersData(editDataSetter);
                    tab = 2;
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Sounds changer by action:", GUILayout.Width(180));
                if (footstepSoundsDataChanger == null)
                {
                    if (GUILayout.Button("Add", GUILayout.Width(55)))
                    {
                        if(footstepsStateController == null)
                        {
                            footstepsStateController = footstepsController.gameObject.AddComponent<FootstepsStateController>();
                        }

                        footstepSoundsDataChanger = footstepSoundDataSetter.gameObject.AddComponent<FootstepSoundsChanger>();
                        SerializedObject footstepDataChangerObject = new SerializedObject(footstepSoundsDataChanger);
                        footstepDataChangerObject.FindProperty("dataSetter").boxedValue = footstepSoundDataSetter;
                        footstepDataChangerObject.ApplyModifiedProperties();

                        footstepsStateControllerObject = new SerializedObject(footstepsStateController);
                        SerializedProperty footstepsStateControllerProperty = footstepsStateControllerObject.FindProperty("changers");
                        footstepsStateControllerProperty.InsertArrayElementAtIndex(0);
                        footstepsStateControllerProperty.GetArrayElementAtIndex(0).boxedValue = footstepSoundsDataChanger;
                        defaultStateProperty = footstepsStateControllerObject.FindProperty("defaultStateType");
                        initDefaultStateOnAwakeProperty = footstepsStateControllerObject.FindProperty("initDefaultStateOnAwake");
                        footstepsStateControllerObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    if (GUILayout.Button("Edit", GUILayout.Width(55)))
                    {
                        currentDataChanger = footstepSoundsDataChanger;
                        dataChangerObject = new SerializedObject(currentDataChanger);
                        dataChangerListProperty = dataChangerObject.FindProperty("stateData");
                        tab = 3;
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(65)))
                    {
                        RemoveChanger(footstepSoundsDataChanger);

                        DestroyImmediate(footstepSoundsDataChanger);
                        footstepSoundsDataChanger = null;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        protected virtual void DrawFootstepEffectsModule()
        {
            GUILayout.Label("Footstep effects", skin.customStyles[4]);
            if (footstepParticleCreator == null)
            {
                if (GUILayout.Button("Add"))
                {
                    GameObject footstepEffectsObject = new GameObject("ParticleEffects");
                    footstepEffectsObject.transform.parent = editFootHandler.transform;

                    footstepParticleCreator = footstepEffectsObject.AddComponent<FootstepParticleEffectCreator>();
                    footstepParticlesDataSetter = footstepEffectsObject.AddComponent<FootstepParticleEffectSetter>();

                    SerializedObject footstepParticleDataSetterObject = new SerializedObject(footstepParticlesDataSetter);
                    footstepParticleDataSetterObject.FindProperty("footstepParticleEffectCreator").boxedValue = footstepParticleCreator;
                    footstepParticleDataSetterObject.ApplyModifiedProperties();

                    footstepEffectsDataObject = new SerializedObject(footstepParticlesDataSetter);
                    effectsDefaultPoolCapacityProperty = footstepEffectsDataObject.FindProperty("effectsPoolDefaultCapacity");

                    effectsMaxPoolSizeProperty = footstepEffectsDataObject.FindProperty("effectsPoolMaxCapacity");

                    footstepCreatorsListProperty.InsertArrayElementAtIndex(0);
                    footstepCreatorsListProperty.GetArrayElementAtIndex(0).boxedValue = footstepParticleCreator;

                    footstepDataSettersListProperty.InsertArrayElementAtIndex(0);
                    footstepDataSettersListProperty.GetArrayElementAtIndex(0).boxedValue = footstepParticlesDataSetter;

                    footHandlerObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (GUILayout.Button("Remove"))
                {
                    footstepEffectsDataObject = null;
                    RemoveModule(footstepParticleCreator, footstepParticlesDataSetter);
                    DestroyImmediate(footstepParticleCreator.gameObject);
                }
            }



            if (footstepParticlesDataSetter != null)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                GUILayout.Label("Footstep particle data setter:", GUILayout.Width(180));
                if (GUILayout.Button("Edit", GUILayout.Width(55)))
                {
                    editDataSetter = footstepParticlesDataSetter;
                    GetEditSettersData(editDataSetter);
                    tab = 2;
                }

                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(effectsDefaultPoolCapacityProperty.displayName, GUILayout.Width(180));
                EditorGUILayout.PropertyField(effectsDefaultPoolCapacityProperty, GUIContent.none);
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(effectsMaxPoolSizeProperty.displayName, GUILayout.Width(180));
                EditorGUILayout.PropertyField(effectsMaxPoolSizeProperty, GUIContent.none);
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label("Effects changer by action:", GUILayout.Width(180));
                if (footstepEffectsDataChanger == null)
                {
                    if (GUILayout.Button("Add", GUILayout.Width(55)))
                    {
                        if (footstepsStateController == null)
                        {
                            footstepsStateController = footstepsController.gameObject.AddComponent<FootstepsStateController>();
                        }

                        footstepEffectsDataChanger = footstepParticlesDataSetter.gameObject.AddComponent<FootstepEffectsChanger>();
                        SerializedObject footstepDataChangerObject = new SerializedObject(footstepEffectsDataChanger);
                        footstepDataChangerObject.FindProperty("dataSetter").boxedValue = footstepParticlesDataSetter;
                        footstepDataChangerObject.ApplyModifiedProperties();

                        footstepsStateControllerObject = new SerializedObject(footstepsStateController);
                        SerializedProperty footstepsStateControllerProperty = footstepsStateControllerObject.FindProperty("changers");
                        footstepsStateControllerProperty.InsertArrayElementAtIndex(0);
                        footstepsStateControllerProperty.GetArrayElementAtIndex(0).boxedValue = footstepEffectsDataChanger;
                        defaultStateProperty = footstepsStateControllerObject.FindProperty("defaultStateType");
                        initDefaultStateOnAwakeProperty = footstepsStateControllerObject.FindProperty("initDefaultStateOnAwake");
                        footstepsStateControllerObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    if (GUILayout.Button("Edit", GUILayout.Width(55)))
                    {
                        currentDataChanger = footstepEffectsDataChanger;
                        dataChangerObject = new SerializedObject(currentDataChanger);
                        dataChangerListProperty = dataChangerObject.FindProperty("stateData");
                        tab = 3;
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(65)))
                    {
                        RemoveChanger(footstepEffectsDataChanger);

                        DestroyImmediate(footstepEffectsDataChanger);
                        footstepEffectsDataChanger = null;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        protected virtual void RemoveChanger(AbstractFootstepsDataChanger changer)
        {
            SerializedProperty changersListProperty = footstepsStateControllerObject.FindProperty("changers");

            for(int elementNumber = 0; elementNumber < changersListProperty.arraySize; elementNumber++)
            {
                if(changersListProperty.GetArrayElementAtIndex(elementNumber).boxedValue as AbstractFootstepsDataChanger == changer)
                {
                    changersListProperty.DeleteArrayElementAtIndex(elementNumber);
                    break;
                }
            }

            footstepsStateControllerObject.ApplyModifiedProperties();
        }

        protected virtual void ApplyFootstepControllerProperties()
        {
            if (footstepControllerObject != null && footstepControllerObject.targetObject != null && footstepControllerObject.hasModifiedProperties)
            {
                footstepControllerObject.ApplyModifiedProperties();
            }
        }

        protected virtual void ApplyFootHandlerProperties()
        {
            if(footstepSoundCreatorObject != null && footstepSoundCreatorObject.targetObject != null && footstepSoundCreatorObject.hasModifiedProperties)
            { 
                footstepSoundCreatorObject.ApplyModifiedProperties();
            }

            if(footprintPoolObject != null && footprintPoolObject.targetObject != null && footprintPoolObject.hasModifiedProperties)
            {
                footprintPoolObject.ApplyModifiedProperties();
            }

            if (footstepEffectsDataObject != null && footstepEffectsDataObject.targetObject != null && footstepEffectsDataObject.hasModifiedProperties)
            {
                footstepEffectsDataObject.ApplyModifiedProperties();
            }

            if (footHandlerObject != null && footHandlerObject.targetObject != null && footHandlerObject.hasModifiedProperties)
            {
                footHandlerObject.ApplyModifiedProperties();
            }
        }

        protected virtual void ApplyGroundDetectorProperties()
        {
            if(groundDetectorObject != null && groundDetectorObject.targetObject != null && groundDetectorObject.hasModifiedProperties)

            groundDetectorObject.ApplyModifiedProperties();
        }

        protected virtual void GetEditSettersData(AbstractFootstepDataSetter setter)
        {
            Type dataType = typeof(FootprintSurfaceData);
            Type setterType = setter.GetType();

            if (setterType == typeof(FootprintDataSetter))
            {
                dataType = typeof(FootprintSurfaceData);
            }
            else if (setterType == typeof(FootstepSoundSetter))
            {
                dataType = typeof(FootstepSoundData);
            }
            else if (setterType == typeof(FootstepParticleEffectSetter))
            {
                dataType = typeof(FootstepParticleEffectData);
            }


            setterObject = new SerializedObject(setter);
            dataList = setterObject.FindProperty("footstepData");

            moduleData = new ReorderableList(setterObject, dataList, true, true, true, true);

            moduleData.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = moduleData.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x + 5, rect.y, rect.width - 177, EditorGUIUtility.singleLineHeight), element, GUIContent.none);

                if (GUI.Button(new Rect(rect.x + rect.width - 165, rect.y, 65, EditorGUIUtility.singleLineHeight), "Edit"))
                {
                    Selection.activeObject = moduleData.serializedProperty.GetArrayElementAtIndex(moduleData.index).boxedValue as AbstractFootstepData;
                }

                if (GUI.Button(new Rect(rect.x + rect.width - 95, rect.y, 90, EditorGUIUtility.singleLineHeight), "Create new"))
                {
                    AbstractFootstepData newData = CreateInstance(dataType) as AbstractFootstepData;
                    string path = EditorUtility.SaveFilePanelInProject("Create new data", "New data", "asset", "asset");

                    if (!string.IsNullOrEmpty(path) && !string.IsNullOrWhiteSpace(path))
                    {
                        AssetDatabase.CreateAsset
                        (
                            newData,
                            path
                        );
                        moduleData.serializedProperty.GetArrayElementAtIndex(moduleData.index).boxedValue = newData;
                    }
                }
            };

            moduleData.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, dataList.displayName);
        }

        protected virtual void ApplyEditSettersData()
        {
            if (dataList != null && setterObject != null && setterObject.targetObject != null && setterObject.hasModifiedProperties)
            {
                setterObject.ApplyModifiedProperties();
            }
        }

        protected Vector2 scrollPositionThirdTab = Vector2.zero;

        protected virtual void ThirdTab()
        {
            if (editFootHandler == null || editDataSetter == null)
            {
                tab = 0;
                FirstTab();
                return;
            }

            GUILayout.Label("Modular Footstem System", skin.customStyles[3]);

            if (GUILayout.Button("Back", GUILayout.Width(60)))
            {
                tab = 1;
            }

            scrollPositionThirdTab = EditorGUILayout.BeginScrollView(scrollPositionThirdTab);

            GUILayout.Label($"Editable foot: {editFootHandler.gameObject.name}", skin.customStyles[4]);
            GUILayout.Label($"Editable setter: {editDataSetter.gameObject.name}", skin.customStyles[4]);

            moduleData.DoLayoutList();

            EditorGUILayout.EndScrollView();
        }

        protected Vector2 scrollPositionFourthTab = Vector2.zero;

        protected virtual void FourthTab()
        {
            GUILayout.Label("Modular Footstem System", skin.customStyles[3]);

            if (GUILayout.Button("Back", GUILayout.Width(60)))
            {
                tab = 1;
            }

            scrollPositionFourthTab = EditorGUILayout.BeginScrollView(scrollPositionFourthTab);

            
            GUILayout.Label($"Editable foot: {editFootHandler.gameObject.name}", skin.customStyles[4]);
            GUILayout.Label($"Editable data changer: {currentDataChanger.gameObject.name}", skin.customStyles[4]);

            EditorGUILayout.PropertyField(dataChangerListProperty, true);

            EditorGUILayout.EndScrollView();

            if(dataChangerObject != null && dataChangerObject.targetObject != null && dataChangerObject.hasModifiedProperties)
            {
                dataChangerObject.ApplyModifiedProperties();
            }
        }
    }
}