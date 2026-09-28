# Glossary — [TÊN GAME]

> **Một khái niệm một tên.** Dùng đúng tên này trong code, doc, tooltip, và khi nói chuyện với GD.
>
> Đây không phải file trang trí. Hai lỗi đắt nhất về đặt tên đã xảy ra thật:
> - **một tên hai nghĩa** — hai thứ khác hẳn cơ chế cùng tên `Careless`, GD đọc panel hiểu sai và
>   phải viết hẳn một tài liệu để phân biệt;
> - **một khái niệm hai tên** — `Priority` vừa nghĩa "thằng thắng" vừa nghĩa "thứ tự chạy".
>
> Đặt tên trùng một tên đã có nghĩa khác ⇒ **escalate**, không tự đặt.

---

## 1. Từ vựng gameplay

| Term | Nghĩa chính xác | Tên trong code | Không phải là |
|---|---|---|---|
| | | | |

## 2. Từ vựng authoring / tool

| Term | Nghĩa | Tên trong code |
|---|---|---|
| | | |

## 3. Từ vựng đo lường

> Mỗi metric ghi rõ **cái nó KHÔNG nói** — đây là chỗ hay bị đọc quá đà.

| Term | Đo bằng gì | Nói lên gì | KHÔNG nói lên |
|---|---|---|---|
| | | | |

## 4. Cặp từ dễ nhầm

> Liệt kê ở đây mọi cặp mà người mới sẽ nhầm.

| Từ A | Từ B | Khác nhau ở |
|---|---|---|
| | | |

## 5. Từ đã bị đổi tên

| Tên cũ | Tên mới | Vì sao đổi | Decision |
|---|---|---|---|
| | | | |

---

## Luật đặt tên

1. Tên phải mô tả **cái nó là**, không phải cái nó *có vẻ* là. Đặt tên theo một tính chất mà model
   không bảo đảm là lỗi (ví dụ gọi một thang là "yếu → mạnh" khi giữa các bậc không có quan hệ trội).
2. Tên hiển thị **≠** khoá tra cứu. Nếu một chuỗi đang được dùng làm key (`Find("X")`), muốn đổi cách
   hiển thị thì thêm hàm `DisplayName(...)` riêng, **không** đổi `Name`.
3. Technical term giữ English kể cả khi nói tiếng Việt.
4. Một từ chỉ được mang một nghĩa trong toàn project. Cần nghĩa thứ hai → đặt từ mới.
