# Canvas_Menu.prefab - Phân Tích Cấu Trúc UI

## 🌳 Sơ Đồ Cấu Trúc Cây Chi Tiết

```
Canvas_Menu (Root)
│
├── [Canvas Components]
│   ├── Canvas (Render Mode: Overlay)
│   ├── CanvasScaler (Scale With Screen Size - 3840x2160)
│   └── GraphicRaycaster
│
└── [Panels_UI Container]
    │
    ├── Panel_GUIGame (Inactive)
    │   │
    │   ├── [Background]
    │   │
    │   ├── Title (Active)
    │   │   ├── Image_Fantasy_RPG
    │   │   ├── Image_Divider
    │   │   ├── Text_GUI pack
    │   │   │
    │   │   └── Controller_Button
    │   │       ├── Button_LB
    │   │       └── Button_RB
    │   │
    │   └── [Other elements]
    │
    ├── Panel_Loading (Inactive)
    │   │
    │   ├── Background
    │   ├── Loading_Bar
    │   │   ├── Background
    │   │   ├── FillArea
    │   │   │   └── Fill
    │   │   └── Handle Area
    │   └── Text_Loading
    │
    ├── Panel_PopUp_Pause (Inactive)
    │   │
    │   ├── ScreenDimmed
    │   │
    │   └── Content
    │       ├── Background (Image_PopUp_Bg)
    │       ├── Border (Image)
    │       │
    │       └── [Buttons]
    │           ├── Button_Continue
    │           ├── Button_Setting
    │           ├── Button_Save Game
    │           ├── Button_Exit
    │           ├── Button_Close
    │           └── Button_Confirm
    │
    └── Panel_GUISetting (Active)
        │
        ├── panel_Gameplay (Active)
        │   ├── Tab_Gameplay
        │   ├── Tab_Play
        │   ├── Tab_Sprint
        │   ├── Tab_Dodge
        │   ├── Tab_Attack
        │   ├── Tab_Heal
        │   ├── Tab_MiniMap
        │   ├── Tab_Sneak/Crouch
        │   ├── Tab_Weapon/ItemWheel
        │   │
        │   └── [ScrollRect + Sliders]
        │
        ├── panel_Graphics (Active)
        │   ├── Tab_Brightness
        │   ├── Tab_Contrast
        │   ├── Tab_Saturation
        │   ├── Tab_MotionBlur
        │   ├── Tab_ChromaticAberration
        │   ├── Tab_Sharening
        │   ├── Tab_COLORGRADING
        │   ├── Tab_DisplayMode
        │   ├── Tab_ScreenResolution
        │   ├── Tab_FrameRate
        │   ├── Tab_VerticalSynchronisation
        │   ├── Tab_HDRMode
        │   │
        │   └── [ScrollRect + Sliders]
        │
        ├── panel_Controller (Active)
        │   ├── Tab_CameraRotateSpeed
        │   ├── Tab_CameraZoomSpeed
        │   ├── Tab_CameraMoveSpeed
        │   ├── Button_LB
        │   ├── Button_RB
        │   │
        │   └── [ScrollRect + Scrollbar Vertical + Sliders]
        │
        └── panel_Audio (Active)
            ├── Tab_MasterVolume
            ├── Tab_MusicVolume
            ├── Tab_SoundEffectsVolume
            ├── Tab_VoiceLanguage
            ├── Tab_BackgroundSound
            ├── Text_MicVolume
            │
            └── [ScrollRect + Sliders]
```

---

## 📊 Tổng Quan Panels

| Panel | Trạng thái | Mô tả |
|-------|------------|-------|
| Panel_GUIGame | Inactive | In-game UI |
| Title | Active | Header với navigation |
| Panel_Loading | Inactive | Loading screen |
| Panel_PopUp_Pause | Inactive | Pause menu popup |
| Panel_GUISetting | Active | Settings panel chính |

---

## 🔗 Cấu Trúc Settings Tabs

```
Panel_GUISetting
│
├── Gameplay Tab (8 settings)
│   ├── Tab_Play
│   ├── Tab_Sprint
│   ├── Tab_Dodge
│   ├── Tab_Attack
│   ├── Tab_Heal
│   ├── Tab_MiniMap
│   ├── Tab_Sneak/Crouch
│   └── Tab_Weapon/ItemWheel
│
├── Graphics Tab (12 settings)
│   ├── Tab_Brightness
│   ├── Tab_Contrast
│   ├── Tab_Saturation
│   ├── Tab_MotionBlur
│   ├── Tab_ChromaticAberration
│   ├── Tab_Sharening
│   ├── Tab_COLORGRADING
│   ├── Tab_DisplayMode
│   ├── Tab_ScreenResolution
│   ├── Tab_FrameRate
│   ├── Tab_VerticalSynchronisation
│   └── Tab_HDRMode
│
├── Controller Tab (5 elements)
│   ├── Tab_CameraRotateSpeed
│   ├── Tab_CameraZoomSpeed
│   ├── Tab_CameraMoveSpeed
│   ├── Button_LB
│   └── Button_RB
│
└── Audio Tab (5 settings)
    ├── Tab_MasterVolume
    ├── Tab_MusicVolume
    ├── Tab_SoundEffectsVolume
    ├── Tab_VoiceLanguage
    └── Tab_BackgroundSound
```

---

## 📁 Thông Tin File

| Thuộc tính | Giá trị |
|------------|---------|
| **Đường dẫn** | `Assets/Artsystack - Fantasy RPG GUI_MENU/Prefab/Canvas_Menu.prefab` |
| **Dung lượng** | ~43,000+ dòng YAML |
| **Canvas Render Mode** | Screen - Overlay (2D) |
| **Canvas Scaler** | Scale With Screen Size |
| **Reference Resolution** | 3840 x 2160 (4K) |

---

## 📊 Thống Kê UI Elements

| Loại Component | Số lượng |
|----------------|----------|
| Text (TMP) | 80+ |
| Image | 120+ |
| Button Component | 17 |
| Slider | 8 |
| Scrollbar | 4 |
| ScrollRect | 6+ |
| Toggle | 0 (Không có) |
| Dropdown | 0 (Không có) |

---

## 🔘 Danh Sách Buttons

### Button Components:
- Button_RestoreDefaults
- Button_Continue
- Button_Close
- Button_Exit
- Button_Confirm
- Button_RB
- Button_Setting
- Button_Save Game
- Button_LB

---

## 📋 Danh Sách Tất Cả UI Elements

### 🔘 Buttons

| Tên GameObject | Panel |
|----------------|-------|
| Button_RestoreDefaults | Settings |
| Button_Continue | - |
| Button_Close | - |
| Button_Exit | PopUp_Pause |
| Button_Confirm | - |
| Button_RB | Controller |
| Button_Setting | PopUp_Pause |
| Button_Save Game | PopUp_Pause |
| Button_LB | Controller |

### 📑 Tabs/Settings

| Tên | Loại |
|-----|------|
| Tab_Interact | UI Element |
| Tab_Play | Gameplay |
| Tab_Saturation | Graphics |
| Tab_Sprint | Gameplay |
| Tab_CameraRotateSpeed | Controller |
| Tab_MoveBackward | KeyBinding |
| Tab_SoundEffectsVolume | Audio |
| Tab_Setting | Settings |
| Tab_Jump | KeyBinding |
| Tab_Gameplay | Settings |
| Tab_MotionBlur | Graphics |
| Tab_Dodge | Gameplay |
| Tab_Brightness | Graphics |
| Tab_Attack | Gameplay |
| Tab_MoveLeft | KeyBinding |
| Tab_MiniMap | Gameplay |
| Tab_Contrast | Graphics |
| Tab_CameraZoomSpeed | Controller |
| Tab_ChromaticAberration | Graphics |
| Tab_MoveForward | KeyBinding |
| Tab_DisplayMode | Graphics |
| Tab_MusicVolume | Audio |
| Tab_VoiceLanguage | Audio |
| Tab_ScreenResolution | Graphics |
| Tab_Sneak/Crouch | Gameplay |
| Tab_VerticalSynchronisation | Graphics |
| Tab_Sharening | Graphics |
| Tab_MoveRight | KeyBinding |
| Tab_Weapon/ItemWheel | Gameplay |
| Tab_CameraMoveSpeed | Controller |
| Tab_Heal | Gameplay |
| Tab_FrameRate | Graphics |
| Tab_COLORGRADING | Graphics |
| Tab_Menu | Navigation |
| Tab_MasterVolume | Audio |
| Tab_HDRMode | Graphics |
| Tab_BackgroundSound | Audio |

### 🎚️ Sliders

- Slider
- Slider (1)
- Slider (2)
- Slider (3)
- Slider (4)
- Slider (5)
- Slider (6)
- Slider (7)

### 📜 Scrollbars

- Scrollbar Vertical
- Scrollbar Vertical (2)
- Scrollbar Vertical (3)
- Scrollbar Vertical (4)

### 🖼️ Images

| Tên | Mô tả |
|-----|-------|
| Image_Split | Divider |
| Image_Fantasy_RPG | Title Background |
| Image_Divider | Title Divider |
| Image_PopUp_Bg | Popup Background |

### 📝 Text Elements

| Tên | Loại |
|-----|------|
| Text_MicVolume | Audio |
| Text_GUI pack | Title |
| Text_Message | Message |
| Text_Info | Info |
| Text_Loading | Loading |

### 📄 Tap_Text Elements

Các text hiển thị trên button/menu (15 instances)

---

## 🎮 Phân Loại Theo Chức Năng

### Gameplay Settings
- Tab_Play
- Tab_Sprint
- Tab_Dodge
- Tab_Attack
- Tab_Heal
- Tab_MiniMap
- Tab_Weapon/ItemWheel
- Tab_Sneak/Crouch

### Graphics Settings
- Tab_Brightness
- Tab_Contrast
- Tab_Saturation
- Tab_MotionBlur
- Tab_ChromaticAberration
- Tab_Sharening
- Tab_COLORGRADING
- Tab_DisplayMode
- Tab_ScreenResolution
- Tab_FrameRate
- Tab_VerticalSynchronisation
- Tab_HDRMode

### Audio Settings
- Tab_MasterVolume
- Tab_MusicVolume
- Tab_SoundEffectsVolume
- Tab_VoiceLanguage
- Tab_BackgroundSound

### Controller Settings
- Tab_CameraRotateSpeed
- Tab_CameraZoomSpeed
- Tab_CameraMoveSpeed
- Button_LB
- Button_RB

### KeyBinding
- Tab_MoveForward
- Tab_MoveBackward
- Tab_MoveLeft
- Tab_MoveRight
- Tab_Jump

---

## 🔧 Thành Phần Kỹ Thuật

### TextMeshPro
- Font: e3f54f2386eacfb498711344712aeeef
- Font Size: 18-72

### CanvasScaler
- Mode: Scale With Screen Size
- Resolution: 3840 x 2160
- Match: Height

### GraphicRaycaster
- Blocking: None
- Ignore Reversed: True

---

## 📝 Ghi Chú

- **KHÔNG CÓ** Toggle và Dropdown trong prefab
- **KHÔNG CÓ** các tabs phụ như Inventory, Character, Skills, Quest, Map, Achievement trong UI chính
- Tất cả Buttons đều chưa có event - cần chạy tool `SetupButtonEvents.cs`

---

## 📊 THỐNG KÊ TỔNG QUAN

| Loại | Số lượng |
|------|----------|
| Panels | 6 |
| Sub-Panels (Settings) | 4 |
| Buttons | 17 |
| Tabs/Labels | 38 |
| Sliders | 8 |
| Scrollbars | 4 |
| Images (đặc biệt) | 7 |
| Text Elements | 10 |
| ScrollRects | 3+ |
| **Tổng GameObjects** | **~1000+** |

---

## 🔢 DANH SÁCH ĐẦY ĐỦ THEO LINE NUMBER

### 1️⃣ PANELS (Line Number)

| Tên | Line | Trạng thái |
|-----|------|-------------|
| Canvas_Menu | 30505 | Root |
| Panels_UI | 4432 | Container |
| Panel_GUIGame | 32310 | Inactive |
| Title | 10654 | Active |
| Panel_Loading | 35473 | Inactive |
| Panel_PopUp_Pause | 2219 | Inactive |
| Panel_GUISetting | 21104 | Active |

### 2️⃣ SETTINGS SUB-PANELS

| Tên | Line |
|-----|------|
| panel_Gameplay | 10942 |
| panel_Graphics | 27816 |
| panel_Controller | 16032 |
| panel_Audio | 37996 |

### 3️⃣ ALL BUTTONS (Full List)

| # | Tên | Line |
|---|-----|------|
| 1 | Button_RestoreDefaults | 323 |
| 2 | Button_Continue | 693 |
| 3 | Button_Close | 1354, 32668, 38822 |
| 4 | Button_Exit | 11254 |
| 5 | Button_Confirm | 15458, 24400, 39846 |
| 6 | Button_RB | 15942 |
| 7 | Button_Setting | 16929, 17171 |
| 8 | Button_Save Game | 22537 |
| 9 | Button_LB | 33448 |
| 10 | Button_RestoreDefaults | 40981, 42659 |
| 11 | Tab_Interact | 634 |
| 12 | Tab_Play | 1604, 19316 |

### 4️⃣ ALL TABS (Full List)

| # | Tên | Line |
|---|-----|------|
| 1 | Tab_Interact | 634 |
| 2 | Tab_Play | 1604, 19316 |
| 3 | Tab_Saturation | 3109 |
| 4 | Tab_Sprint | 3377 |
| 5 | Tab_CameraRotateSpeed | 9076 |
| 6 | Tab_MoveBackward | 9207 |
| 7 | Tab_SoundEffectsVolume | 9263 |
| 8 | Tab_Setting | 9507, 19121, 37106, 39218 |
| 9 | Tab_Jump | 9851 |
| 10 | Tab_Gameplay | 10156, 17900, 36059, 39687 |
| 11 | Tab_MotionBlur | 10982 |
| 12 | Tab_Dodge | 12304 |
| 13 | Tab_Brightness | 14059 |
| 14 | Tab_Attack | 14115 |
| 15 | Tab_MoveLeft | 14548 |
| 16 | Tab_MiniMap | 15671 |
| 17 | Tab_Contrast | 16811 |
| 18 | Tab_CameraZoomSpeed | 17334 |
| 19 | Tab_ChromaticAberration | 17465 |
| 20 | Tab_MoveForward | 18371 |
| 21 | Tab_DisplayMode | 19450 |
| 22 | Tab_MusicVolume | 19845 |
| 23 | Tab_VoiceLanguage | 21728 |
| 24 | Tab_ScreenResolution | 22129 |
| 25 | Tab_Sneak/Crouch | 24165 |
| 26 | Tab_VerticalSynchronisation | 24574 |
| 27 | Tab_Sharening | 27379 |
| 28 | Tab_MoveRight | 28319 |
| 29 | Tab_Weapon/ItemWheel | 28649 |
| 30 | Tab_CameraMoveSpeed | 29127 |
| 31 | Tab_Heal | 30132 |
| 32 | Tab_FrameRate | 31230 |
| 33 | Tab_COLORGRADING | 34730 |
| 34 | Tab_Menu | 37211 |
| 35 | Tab_HDRMode | 42293 |
| 36 | Tab_MasterVolume | 42237 |
| 37 | Tab_BackgroundSound | 42349 |

### 5️⃣ SLIDERS

| # | Tên | Line |
|---|-----|------|
| 1 | Slider | 20769 |
| 2 | Slider | 22006 |
| 3 | Slider (1) | 6777 |
| 4 | Slider (1) | 13936 |
| 5 | Slider (1) | 26170 |
| 6 | Slider (1) | 26510 |
| 7 | Slider (1) | 38238 |
| 8 | Slider (1) | 39059 |

### 6️⃣ SCROLLBARS

| # | Tên | Line |
|---|-----|------|
| 1 | Scrollbar Vertical | 15035 |
| 2 | Scrollbar Vertical | 15316 |
| 3 | Scrollbar Vertical | 16150 |
| 4 | Scrollbar Vertical | 24633 |

### 7️⃣ SPECIAL IMAGES

| Tên | Line |
|-----|------|
| Image_Fantasy_RPG | 19771 |
| Image_Divider | 35076 |
| Image_Split | 14605, 24775, 41499, 43765 |
| Image_PopUp_Bg | 43183 |

### 8️⃣ SPECIAL TEXT

| Tên | Line |
|-----|------|
| Text_GUI pack | 3664 |
| Text_Message | 12031 |
| Text_Loading | 38362 |
| Text_MicVolume | 498, 12361, 19179 |
| Text_Info | 12813, 31362, 33958, 34186 |

### 9️⃣ NAVIGATION

| Tên | Line |
|-----|------|
| Controller | 4295, 5749 |
| Graphics | 4707 |
| Gameplay | 5047 |
| Audio | 5185, 5323 |
| Main | 14994 |

### 🔟 OTHER CONTAINERS

| Loại | Line |
|------|------|
| ScreenDimmed | 16292 |
| ScrollRect | 7902, 14928 |
| Content | 18641 |
| Background | 3854 |
| Bg border | 8382, 9702, 13237 |
| Border | 9777, 11956 |
- Tất cả Buttons đều chưa có event - cần chạy tool `SetupButtonEvents.cs`
