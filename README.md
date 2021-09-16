## CSD 属性列表

- GameProjectFile (`DEMO.csd`)
    - [x]  Animation — AnimationClip (`*DEMO_aniname.anim`)
        - [x]  `[A]` ActivedAnimationName — stateMachine 's default state
        - [ ]  `[A]` Duration — ignored
        - [x]  `[A]` Speed — calc a param: frameIndex to time
        - [x]  *Timeline
            - [x]  `[A]` ActionTag — to link the game object
            - [x]  `[A]` Property
                - [x]  `="Position"` — link `RectTransform->m_AnchoredPosition`
                - [x]  `="Scale"` — link `RectTransform->m_LocalScale`
                - [x]  `="AnchorPoint"` — link `RectTransform->m_Pivot`
                - [x]  `="VisibleForFrame"` — link `GameObject->m_IsActive`
                - [ ]  `="RotationSkew"` —  link `RectTransform->m_LocalRotation`  **(!Ignore Skew)**
                - [x]  `="FileData"` — link `Image->m_Sprite`
                - [x]  `="Alpha"` — link `Image->m_Color.a`
                - [ ]  `="ActionValue"` —  sub anim behaviour anim? **(!Not implement)**
            - [x]  XXXFrame
                - [x]  `[A]` FrameIndex — Frame Index
                - [x]  `[A]` Tween — Constant Easing Type
                - [x]  EasingData — describe the curve shape
                    - [x]  `[A]` Type
                        - [ ]  `="-1"` — Custom Easing Type **(!Use Linear)**
                            - [ ]  Points
                                - [ ]  *PointF
                        - [x]  `="0"` — Linear Easing Type
                        - [ ]  `="1"` — Sine_EaseIn Easing Type  **(!Similarly use Unity Smooth In)**
                        - [ ]  `="2"` — Sine_EaseOut Easing Type  **(!Similarly use Unity Smooth Out)**
                        - [ ]  `="4"` — Quad_EaseIn Easing Type  **(!Similarly use Unity Smooth In)**
                        - [ ]  `="5"` — Quad_EaseOut Easing Type  **(!Similarly use Unity Smooth Out)**
                        - [ ]  `="7"` — Cubic_EaseIn Easing Type  **(!Similarly use Unity Smooth In)**
                        - [ ]  `="8"` — Cubic_EaseOut Easing Type  **(!Similarly use Unity Smooth Out)**
    - [x]  AnimationList — AnimatorController (`DEMO.controller`)
        - [x]  *AnimationInfo
            - [x]  `[A]` Name — state name / `aniname`
            - [x]  `[A]` StartIndex — cut the main timeline from
            - [x]  `[A]` EndIndex — cut the main timeline to
            - [ ]  RenderColor — ignored, just use in cocos editor
    - [x]  ObjectData — GameObject (`DEMO.prefab`)
        - [x]  `[A]` Name — `gameObject.name`
        - [ ]  `[A]` ~~Tag~~ — ignored, unique key
        - [x]  `[A]` ActionTag — to link the timeline
        - [x]  `[A]` VisibleForFrame — `gameObject.activeSelf`
        - [x]  `[A]` TouchEnable — `gameObject.Image.raycastTarget`
        - [ ]  `[A]` CanEdit — ignored, just use in cocos editor
        - [ ]  `[A]` ~~Rotation~~ — ignored, same as "*RotationSkewX*"
        - [ ]  `[A]` RotationSkewX — `gameObject.RectTransform.localRotation.z` **(!Ignore Skew)**
        - [ ]  `[A]` RotationSkewY — `gameObject.RectTransform.localRotation.z` **(!Ignore Skew)**
        - [x]  `[A]` HorizontalEdge — `gameObject.RectTransform.anchorMin/anchorMax`
        - [x]  `[A]` VerticalEdge — `gameObject.RectTransform.anchorMin/anchorMax`
        - [x]  `[A]` LeftMargin — `gameObject.RectTransform.anchorMin/anchorMax`
        - [x]  `[A]` RightMargin — `gameObject.RectTransform.anchorMin/anchorMax`
        - [x]  `[A]` TopMargin — `gameObject.RectTransform.anchorMin/anchorMax`
        - [x]  `[A]` BottomMargin — `gameObject.RectTransform.anchorMin/anchorMax`
        - [ ]  IconVisible — ignored, dont know the usage
        - [ ]  `[A]` Alpha — `gameObject.Image.color.a` **(!Affects children)**
        - [ ]  CColor — `gameObject.Image.color` **(!Affects children)**
        - [ ]  PreSize — ignored, dont know the usage
        - [x]  Size — `gameObject.RectTransform.sizeDelta`
        - [x]  AnchorPoint — `gameObject.RectTransform.pivot`
        - [ ]  PrePosition — ignored, dont know the usage
        - [x]  Position — `gameObject.RectTransform.anchoredPosition`
        - [x]  Scale — `gameObject.RectTransform.localScale`
        - [x]  `[A]` ctype — type of the node
            - [x]  `="GameNodeObjectData"` — the root node
            - [x]  `="PanelObjectData"` — the empty (just background colors) node
                - [x]  `[A]` ComboBoxIndex
                - [x]  `[A]` BackColorAlpha
                - [ ]  `[A]` ~~ColorAngle~~ — ignored, same as "ColorVector*"*
                - [x]  SingleColor
                - [ ]  FirstColor — **(!Gradient color system)**
                - [ ]  EndColor — **(!Gradient color system)**
                - [ ]  ColorVector — **(!Gradient color system)**
            - [x]  `="SpriteObjectData"` — the image node
                - [x]  FileData(`Type="MarkedSubImage"`) — `gameObject.Image.sprite`
            - [x]  `="ProjectNodeObjectData"` — the linked node
                - [x]  FileData(`Type="Normal"`) — GameObject linked to another ".prefab"
        - [x]  Children — Child GameObject s
