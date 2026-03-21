# Portal plane clip (URP)

## Shader
`DungeonMania/URP/PortalPlaneClipLit` — mesh bị **ẩn một nửa không gian** và **lộ dần** khi xuyên qua mặt phẳng cổng (soft edge theo `_PortalSoftness`).

- **Visible side:** nửa không gian mà `dot(worldPos - portalPoint, portalNormal) > 0` (sau khi normalize, và có thể đảo bằng `_PortalInvert`).
- **Material mẫu:** `Assets/_DungeonMania/Materials/PortalPlaneClip_Lit.mat`

## Script
`PortalPlaneClipBinder` — mỗi frame (hoặc một lần) gán `_PortalPoint` / `_PortalNormal` qua `MaterialPropertyBlock` lên các `Renderer` của boss.

1. Gán material shader trên **từng material slot** của boss (thay material cũ hoặc duplicate texture).
2. Thêm `PortalPlaneClipBinder` lên boss, kéo **Transform cổng** (nên là điểm đúng mặt phẳng cổng).
3. Nếu boss **ẩn nhầm bên**, tích **Invert Visible Side** hoặc đổi `Use Inverse Forward As Normal`.
4. Chỉnh **Portal Softness** (world units) để mép mềm.

## Gizmo
Khi chọn object có binder, trong Scene view có đoạn gizmo cyan từ `portalPlane.position` theo hướng normal để kiểm tra.
