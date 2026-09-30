---
name: game-feel-motion
description: >
  Biến mô tả cảm tính về chuyển động ("chưa đã tay", "cứng quá", "giống video này") thành spec
  chuyển động implement được — bằng English technical term + visualiser để hai bên hình dung cùng
  một thứ. KÍCH HOẠT khi: bàn về anim/tween/movement của element, có video/gif reference, juice pass,
  "animation này chưa ổn", hoặc cần chọn easing/timing/curve. Dùng kèm knowledge/motion/.
---

# SKILL: game-feel-motion

Vấn đề cốt lõi: **"chưa đã tay" không implement được.** Skill này tồn tại để dịch cảm giác sang
từ vựng chung, rồi sang số.

Ba luật:

1. **Luôn trả lời bằng English technical term** (`anticipation`, `overshoot`, `settle`, `ease-out
   back`, `squash & stretch`), kèm giải thích ngắn tiếng Việt. Term lấy từ `knowledge/motion/vocabulary.md`.
2. **Luôn dựng visualiser trước khi hỏi** — chuyển động không mô tả được bằng lời.
   Nền: `knowledge/motion/playground.html`.
3. **Mọi số cuối cùng nằm ở `MotionProfile`/`TimingProfile`**, component chỉ giữ `preset id`.
   Không hardcode duration/easing trong code. (`AGENTS.md §5`)

---

## Bước đầu tiên — đọc context

`knowledge/motion/vocabulary.md` (từ vựng) · `knowledge/motion/presets.json` (preset đã có) ·
`Docs/project-context.md` (tween lib đang dùng, profile nào giữ số) · `knowledge/feel/checklist.md`.

**Trước khi đề xuất chuyển động mới → tra `presets.json`.** Có preset gần đúng thì tinh chỉnh preset,
đừng đẻ preset thứ 12 gần giống preset thứ 3. Preset trùng nhau là cách nhanh nhất để game mất
tính nhất quán.

---

## Flow

### Bước 0 — phân loại yêu cầu

| Loại | Dấu hiệu | Đi tiếp |
|---|---|---|
| **A. Có video/gif ref** | dev gửi link/file | → `refs/video-ref-analysis.md` |
| **B. Mô tả cảm tính** | "chưa đã", "cứng", "nặng nề" | → Bước 1 (dịch cảm giác) |
| **C. Có sẵn anim, cần tinh chỉnh** | "nhanh quá / chậm quá" | → Bước 3 (visualiser so sánh) |
| **D. Element mới, chưa có gì** | "làm anim cho X" | → Bước 2 (chọn từ vocabulary) |

### Bước 1 — dịch cảm giác sang technical term

Không hỏi *"anh muốn nó thế nào?"* — dev đã nói rồi, chỉ là bằng ngôn ngữ khác. Việc của skill là
**đề xuất bản dịch**, dev sửa.

Bảng dịch hay dùng:

| Dev nói | Thường có nghĩa là | Cần chỉnh |
|---|---|---|
| "cứng", "khô" | thiếu `anticipation` / `follow-through` | thêm pha chuẩn bị + đuôi |
| "chưa đã tay", "chưa sướng" | thiếu `overshoot` + `impact` cue | ease-out back, scale punch, VFX/audio |
| "nặng nề", "lê thê" | duration quá dài hoặc ease sai | giảm duration, đổi sang ease-out |
| "giật cục" | thiếu `settle`, hoặc cắt giữa tween | thêm pha lắng, cho phép tween nối |
| "trôi tuột", "không có trọng lượng" | thiếu `squash & stretch` / accel-decel | thêm biến dạng theo vận tốc |
| "rối mắt" | quá nhiều thứ chuyển động cùng lúc | `stagger`, giảm biên độ phụ |
| "chậm phản hồi" | thiếu phản hồi **tức thì** ở frame đầu | tách immediate cue khỏi anim chính |

> Dịch xong **luôn trình bày lại** dạng: *"Em hiểu là cần thêm `anticipation` (≈100ms nén trước khi
> bật) + `overshoot` 15% rồi `settle`. Đúng ý anh chưa?"* — kèm visualiser.

### Bước 2 — mô tả chuyển động theo khung 5 pha

Mọi chuyển động trong game viết được bằng khung này. Pha nào không có thì ghi `—`.

| Pha | Câu hỏi | Ví dụ |
|---|---|---|
| `anticipation` | có chuẩn bị trước khi động không? | nén xuống trước khi nhảy |
| `action` | chuyển động chính: từ đâu → đâu, bao lâu, ease gì | bay lên 0.35s ease-out-cubic |
| `overshoot` | có vượt đích rồi quay lại không? bao nhiêu % | vượt 12% |
| `settle` | lắng về trạng thái cuối thế nào | 0.12s ease-out |
| `follow-through` | phần phụ trễ hơn phần chính | đuôi/áo/particle trễ 0.05s |

Kèm bảng số:

```markdown
### Motion spec — [element] / [hành động]

| Pha | Duration | Easing | Từ → Đến | Preset id |
|---|---|---|---|---|

**Trigger:** ...
**Interrupt policy:** (bị gọi lại giữa chừng thì làm gì — xem mục Interrupt bên dưới)
**Nơi giữ số:** MotionProfile.<field>   ← không hardcode
**Cue đi kèm:** VFX ... / Audio ... / Haptic ...
```

### Bước 3 — dựng visualiser rồi hỏi trắc nghiệm

Bắt buộc dựng `.html` khi: chọn easing · chọn duration · so sánh 2–3 phương án · chuyển động có
nhiều phần tử (stagger) · timing liên hoàn.

Yêu cầu visualiser (chi tiết ở `workflow/ask-and-visualise.md`):

- Chạy được **cạnh nhau**, cùng lúc, để so sánh — không phải chạy lần lượt rồi nhớ lại.
- Có slider chỉnh duration / overshoot / stagger **ngay trên trang**.
- Hiển thị **số hiện tại** (dev sẽ đọc số đó ra và chốt).
- Có nút replay và có chế độ slow-motion 0.25×.
- Nhãn mỗi phương án bằng technical term, không phải "Option A/B/C".

Rồi hỏi trắc nghiệm 2–4 phương án + đúng một recommend + lý do.

### Bước 4 — chốt số → đẩy vào profile → ghi lại

1. Số chốt ghi vào `MotionProfile` (hoặc `TimingProfile` nếu là nhịp gameplay).
2. Preset dùng lại được ⇒ thêm vào `knowledge/motion/presets.json` với `id`, `phases`, `khi nào dùng`.
3. Ghi vào `implementation-notes.html`: đã loại phương án nào, vì sao.

---

## Interrupt policy — chỗ hay sinh bug nhất

Mọi tween phải khai báo trước: **bị gọi lại khi đang chạy thì làm gì?**

| Policy | Nghĩa | Dùng khi |
|---|---|---|
| `restart` | kill tween cũ, chạy lại từ đầu | feedback ngắn, lặp nhanh (nút bấm) |
| `ignore` | đang chạy thì bỏ qua lệnh mới | anim quan trọng không được cắt |
| `queue` | xếp hàng chạy sau | chuỗi có thứ tự |
| `blend` | tiếp tục từ giá trị hiện tại | di chuyển liên tục, đổi đích giữa đường |

Luật cứng:

- Tween **phải bị kill khi element về pool / level unload** — nếu không, tween tiếp tục ghi vào
  transform của object đã tái sử dụng. Đây là bug "object nhảy loạn sau khi respawn".
- Pooled visual dùng contract hai đầu:
  **Release** = kill/cancel + invalidate owner/generation + normalize state an toàn;
  **Acquire/Bind** = ghi lại đầy đủ scale/rotation/color/visibility/outline/count từ data hiện tại.
  Acquire không được tin rằng Release trước đã chạy hoàn hảo.
- Tween/task completion cũ chỉ được settle/reset nếu `operationId/generation` vẫn current; motion A bị thay bởi B không được clear state của B.
- Domain **không đợi tween**. Domain giữ nhịp bằng scheduler của nó (`standards/system-design.md §6`);
  Visual chạy tween song song. Nếu gameplay phải đợi anim xong ⇒ đó là **duration trong TimingProfile**,
  không phải callback của tween.

---

## Feedback tức thì vs anim chính

Luật cảm giác quan trọng nhất trên mobile: **phản hồi phải xuất hiện trong frame đầu tiên.**

Tách hai thứ:

| Lớp | Độ trễ | Ví dụ |
|---|---|---|
| `immediate cue` | 0 frame | đổi màu, scale punch nhỏ, audio click, haptic |
| `main motion` | 0.15–0.5s | di chuyển, xoay, bay tới đích |

Chỉ có `main motion` mà không có `immediate cue` ⇒ dev sẽ nói *"chậm phản hồi"* kể cả khi duration
đã ngắn.

---

## Ngân sách thời lượng (điểm khởi đầu, không phải luật)

| Loại | Duration gợi ý |
|---|---|
| micro feedback (nút, highlight) | 0.08 – 0.15s |
| element di chuyển ngắn | 0.2 – 0.35s |
| element bay xa / theo curve | 0.35 – 0.6s |
| chuyển state UI/panel | 0.2 – 0.3s |
| celebration / win sequence | 0.8 – 2.0s (có skip) |
| stagger giữa các phần tử | 0.03 – 0.08s/phần tử |

Trên mobile, người chơi lặp lại hành động hàng trăm lần ⇒ **thà ngắn hơn 20% còn hơn dài hơn 20%**.
Mọi sequence dài > 1s phải **skip được bằng tap**.

---

## Bẫy hay gặp

| Bẫy | Hậu quả |
|---|---|
| hardcode duration trong script | GD không tune được, mỗi chỗ một số |
| mỗi element một preset riêng | game mất nhất quán, không nhận ra "ngôn ngữ chuyển động" |
| tween không kill khi pool/unload | object nhảy loạn, ghi đè transform |
| domain `await` tween của visual | gameplay khựng khi tween bị cắt; test không chạy được headless |
| chỉ chỉnh easing mà không chỉnh duration | vẫn "chưa đã" — hai biến này phải chỉnh cùng nhau |
| thêm juice khắp nơi cùng lúc | rối mắt, không biết cái nào tạo hiệu quả |
| so sánh phương án bằng lời | mỗi bên hình dung một kiểu, chốt xong vẫn sai |
| anim dài trong vòng lặp chính | phá nhịp, người chơi sốt ruột ở lần thứ 50 |

---

## Đầu ra

1. Bảng **Motion spec** 5 pha + số + preset id.
2. `.html` visualiser (lưu ở `handoff/<story>/`).
3. Preset mới đẩy vào `knowledge/motion/presets.json` (nếu dùng lại được).
4. Số chốt nằm trong `MotionProfile` / `TimingProfile`, **không** trong code.
5. Motion thuộc visual pass (có lab + direction): số đi qua `sections.motion` của direction → Import, và kiểm bằng `MotionParity` (`skills/visual-review/`, `workflow/visual-direction.md`). Không chỉnh tay field đã có trong `profileMap`.

Trước khi đóng story feel: chạy `knowledge/feel/checklist.md`.
