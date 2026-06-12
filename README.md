# Tên dự án: Eryndor

Khóa luận tốt nghiệp / Đồ án Capstone - Group 9  
**VTC Academy** — Lớp K24GD-01

## I. Giới thiệu tổng quan

**Eryndor** là tựa game **Action RPG 3D** được phát triển trên nền tảng Unity, kết hợp giữa **thế giới bán mở (Semi Open World)** và **lối chơi tập trung vào dungeon (Dungeon-based Gameplay)**.

Người chơi khám phá bản đồ trung tâm `Map_Chinh`, nhận nhiệm vụ, dịch chuyển vào các hầm ngục theo mốc tiến trình, chiến đấu với quái vật và Boss, thu thập tài nguyên để nâng cấp vũ khí và bản thân thay vì dựa vào hệ thống cấp độ nhân vật truyền thống.

## II. Tính năng nổi bật

* **Hệ thống Dungeon:** Bản đồ hầm ngục được thiết kế theo luồng tiến trình rõ ràng từ Hub World qua Portal UI, wave combat, đến màn hình phần thưởng và quay về map chính (chi tiết xem tại [`dungeon_flow_diagram_1775151528357.png`](dungeon_flow_diagram_1775151528357.png)).
* **Hệ thống Boss & AI:** Spawn Boss theo wave, AI quái vật (patrol/chase/attack), kỹ năng đặc biệt của Mage (Shield), và triển khai network cho Boss (Photon Fusion).
* **Hệ thống Thợ rèn (Blacksmith UI):** Nâng cấp vũ khí (Refinement) và gắn ngọc (Socketing) thông qua NPC Blacksmith trong World.
* **Combat & Game Feel:** FSM nhân vật, 3 hệ vũ khí (Kiếm / Rìu / Trượng), Smart Soft Lock-on, hệ thống skill và auto-aim tối ưu hiệu năng.
* **Đồ họa & Hiệu ứng:** Phong cách Steampunk / Medieval Fantasy, custom shader (`PortalPlaneClipLit`), VFX chiến đấu và UI được tối ưu trên URP.
* **Hệ thống phụ trợ:** Item drop orb, màn hình loading có fake progress khi chuyển cảnh network, nhạc nền động theo trạng thái Boss, minimap và save/load.

## III. Thành viên thực hiện (Group 9)

* **Phan Thành Nam** : World building (Map, NPC, Quests, World BGM)
* **Tô Vũ Kiệt** : Programmer (Player's states, Inventory and items, Network, Cutscene, Abilities system, HP, Dungeons's BGM, Player's sfx)
* **Phạm Trí Hiển** : UI Design / Enemies logic (Menu design, Enemies AI, UI Black Smith, UI sfx)
* **Nông Hoàng Nam** : Dungeon / Rewards (Dungeon building, Items drop, waves management, Enemies sfx)

## IV. Công nghệ sử dụng

* **Engine:** Unity 6 (6000.0.70f1), Universal Render Pipeline (URP)
* **Ngôn ngữ:** C#, ShaderLab, HLSL
* **Networking:** Photon Fusion
* **Công cụ hỗ trợ:** Cinemachine, Input System, NavMesh, Git / GitHub (Git LFS)
* **IDE:** Visual Studio 2022

## V. Hình ảnh Demo

![Icon Eryndor](Assets/AI_Generated_Frames/Icon%20game%20Eryndor.png)

![Dungeon Flow Diagram](dungeon_flow_diagram_1775151528357.png)

![World Timeline](Document_capstoneproject/world_timeline_eryndor.png)
