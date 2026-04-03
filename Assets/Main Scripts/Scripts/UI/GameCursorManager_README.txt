Setup System Cursor (GameCursorManager)
=======================================

1. Import cursor textures:
   - Chọn texture dùng làm cursor.
   - Texture Type = Cursor.

2. Set cursor mặc định trong Project Settings:
   - Project Settings > Player > Default Cursor.
   - Đây là cursor mặc định khi GameCursorManager không override.

3. Trong scene:
   - Tạo Empty GameObject tên "GameCursorManager".
   - Add component GameCursorManager.

4. Trong Inspector của GameCursorManager:
   - (Optional) Normal Cursor Texture.
     Nếu để trống, script sẽ dùng default cursor từ Player Settings.
   - (Optional) Button Hover Cursor Texture.
   - (Optional) Item Hover Cursor Texture.
   - (Optional) Settings Hover Cursor Texture.
   - Set hotspot cho từng trạng thái nếu cần.

5. Chạy game:
   - Khi rê vào Button => đổi cursor button hover (nếu có gán).
   - Khi rê vào ItemUI/GemItemUI/EquipmentItemUI => đổi cursor item hover (nếu có gán).
   - Khi không hover gì => về normal/default cursor.

6. (Optional) Với UI đặc biệt:
   - Add component CursorHoverTarget và chọn Hover Type = Button, Item hoặc Settings.
   - Với nút Settings trong Menu: gắn CursorHoverTarget lên nút đó và chọn Hover Type = Settings.

Lưu ý:
- Script tự DontDestroyOnLoad.
- Script chỉ override cursor khi chuột ở trạng thái tương tác (unlock hoặc visible), nên đồng bộ với Alt và inventory.
