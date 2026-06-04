# Máy móc & Thiết bị — Machine Management

> Tài liệu này tổng hợp phân tích từ legacy Vinam-MES database, so sánh với Epicor ERP, và định hướng implement cho Phase 5.

---

## 1. Legacy Database — Cấu trúc

### 1.1 Bảng `factory`

Vinam có **2 nhà máy** — dữ liệu lưu trong bảng `factory`:

| FactoryID | Tên | Địa chỉ | Số máy |
|---|---|---|---|
| 1 | Vinam Dĩ An Chí | 29 Đường Dĩ An Chí, Thủ Dầu Một | 97 máy |
| 2 | Vinam Mỹ Phước | C_1B_C2, Lô C_1B_CN, KCN Mỹ Phước 3, Bến Cát, Bình Dương | 23 máy |

### 1.2 Bảng `machinegroup` — 56 nhóm

```sql
MachineGroup (PK int)
GroupCode    varchar(45)   ← identifier chính, khớp với machine.MachineType
GroupName    varchar(100)
```

Danh sách đầy đủ 56 nhóm: 5MI, ASY, BDG, BTA, CGP, COP, CPO, DMC, EDM, ENG, FLUO, FRT, FTR, GDM, GMO, GRP, HMP, HNG, HTP, HTR, IDH, INS, ISS, IST, LAP, LLA, LLA35, LLA40, LLA60, MAC, MAL, MAM, MIL, MLA36, MLA60, MLK, NCO, NDE, PMP, PNG, POP, PPG, PRT, PTH, QPQ, SAW, SLA, SRP, STP, TLA, UTO, VAC, WDP, WED, WIT, XYL.

**2 nhóm bị thiếu** (máy Victor thêm sau, chưa tạo group): `VMM2`, `VML2`.

### 1.3 Bảng `machine` — 120 máy (115 active)

```sql
MachineID         int PK
MachineCode       varchar(50) UNIQUE  ← identifier chính, dùng trong session/MQTT
MachineName       varchar(100)
MachineType       varchar(50)         ← FK ngầm → machinegroup.GroupCode
MachineSerial     varchar(200)
MaxOD             varchar(50)         ← đường kính gia công tối đa
Length            varchar(50)         ← chiều dài chi tiết tối đa
DIA               varchar(50)         ← đường kính lỗ suốt
X / Y / Z         varchar(50)         ← hành trình các trục (mm)
AB / C            varchar(50)         ← hành trình trục quay (độ)
PowerSource       varchar(50)         ← nguồn điện (AC 220, AC 380...)
RateCapacity      varchar(50)         ← công suất định mức (kW)
FullLoadCurrent   varchar(50)         ← dòng toàn tải (A)
NetWeight         varchar(50)         ← khối lượng máy (kg)
DateOfManufacture date                ← năm sản xuất
FactoryID         int                 ← FK → factory
CNCMachine        int                 ← 1=CNC, 0=thủ công/process
```

**Lưu ý:** Hầu hết thông số kỹ thuật (MaxOD, X/Y/Z travel, PowerSource...) đều NULL trong thực tế — trường tồn tại nhưng chưa bao giờ được nhập đầy đủ trong legacy. Chỉ có máy `TEST` có dữ liệu đầy đủ (dùng để test nhập liệu).

---

## 2. Quan hệ 3 chiều — Phát hiện quan trọng nhất

`machinegroup.GroupCode` = `optype.TypeCode` = `machine.MachineType` — **ba bảng cùng dùng một namespace code**.

```
partop.OPType ──── machinegroup.GroupCode ──── machine.MachineType
    "MLA"                "MLA"                      "MLA"
                   MEDIUM LATHE              400ML1, 400ML2, 400ML3,
                                             4100-1, 4100-2, 4100-3,
                                             4100-4, 5100, MORISEIKI...
```

Đây chính là **ResourceGroup concept của Epicor ERP**:
- Epicor: `PartOpr.ResourceGrpID` → `ResourceGroup` → `Resource` (từng máy)
- Legacy Vinam: `PartOp.OPType` → `machinegroup.GroupCode` → `machine.MachineType`

**Ý nghĩa:** Khi một PartOp có `OPType = "MLA"` → operator có thể thực hiện trên **bất kỳ máy nào** thuộc nhóm MLA (không cố định 1 máy). Máy cụ thể nào thực sự gia công sản phẩm được ghi lại trong `measurevalue.MachineID`.

### Sơ đồ quan hệ đầy đủ

```
factory (FactoryID, Name)
  └── machine (MachineID, MachineCode, MachineType, FactoryID)
                                  │
                                  ▼ khớp GroupCode
              machinegroup (GroupCode, GroupName)
                                  │
                                  ▼ khớp OPType  
              partop (OPID, OPType, PartID, ...)
                                  │
                                  ▼
              measurevalue (MachineID → machine.MachineID)
```

**Không có FK thực sự** trong legacy — quan hệ là string matching, không có constraint.

---

## 3. Phân nhóm máy theo chức năng

### Nhóm gia công CNC chính

| GroupCode | Tên | Máy tiêu biểu | Số máy |
|---|---|---|---|
| LLA, LLA35, LLA40, LLA60 | Long Lathe (Tiện dài) | 300L, 3100L, 3100XL, 400LLT | 7 |
| MLA, MLA36, MLA60 | Medium Lathe (Tiện trung) | 400ML1/2/3, 4100 series, 5100 | 10 |
| SLA | Small Lathe (Tiện nhỏ) | 300LT1/2, QT-250II, QT-350II | 5 |
| TLA | Tiny Lathe (Tiện nhỏ) | GT2100, QTS-150-S, LEO1600 | 3 |
| VML2 | Victor Lathe 40 | Vturn40-220-x, Vturn40-325-x | 6 |
| VMM2 | Victor Lathe 45 | Vturn45-125-x | 8 |
| MIL | Milling | DMV650, DNM5700, MV6030, VTV-200C, MTV-515/655 | 6 |
| 5MI | 5-axis Milling | 5MI | 1 |
| WED | Wirecut EDM | WED, WED1, WED2 | 3 |
| EDM | EDM | EDM | 1 |
| GDM | Gun Drilling | GDM1 | 1 |
| HNG | Honing | HNG1 | 1 |
| BTA | Deep Hole Drilling | BTA1 | 1 |

### Nhóm kiểm tra chất lượng

| GroupCode | Máy | Ghi chú |
|---|---|---|
| INS | QC-01 đến QC-08, INS, INS1 | PC kiểm tra FAI, CMM... |

### Nhóm process / gia công ngoài

Tất cả các nhóm còn lại (PPG, CGP, HTR, QPQ, XYL, MLK, FLUO, DMC, NCO, BDG, NDE, UTO, PTH, WDP, SRP, PNG, LAP, STP, PMP, PRT, FRT, VAC, WIT, ASY...) là các công đoạn process (phủ bề mặt, nhiệt luyện, kiểm tra đặc biệt, lắp ráp). Nhiều công đoạn gia công ngoài (outside process).

---

## 4. Dữ liệu thực tế — Top máy theo measure count

Dữ liệu từ `measurevalue.MachineID` — 904,699 records:

| Rank | MachineCode | MachineType | Measure Count | % tổng |
|---|---|---|---|---|
| 1 | 3100L | LLA40 | 41,326 | 4.6% |
| 2 | DNM5700 | MIL | 40,826 | 4.5% |
| 3 | MTV-655-60N | MIL | 39,291 | 4.3% |
| 4 | 300L | LLA40 | 35,187 | 3.9% |
| 5 | VTV-200C | MIL | 34,714 | 3.8% |
| 6 | MTV-515-40N | MIL | 32,605 | 3.6% |
| 7 | QT-350II | SLA | 30,964 | 3.4% |
| 8 | MV6030 | MIL | 29,586 | 3.3% |
| 9 | 400ML3 | MLA36 | 27,885 | 3.1% |
| 10 | 400ML2 | MLA36 | 27,249 | 3.0% |

**Quan sát:** Milling machines (MIL) chiếm tỷ trọng lớn trong dữ liệu đo FAI, cùng với Long/Medium Lathe.

---

## 5. Máy chạy cross-type — Pattern thực tế

Dữ liệu production cho thấy máy thực tế gia công các loại OP khác với MachineType của mình:

| Máy | MachineType (group) | OPType đã chạy (từ measurevalue) |
|---|---|---|
| 3100L | LLA40 | **LLA** (5,735), **MLA** (2,854), **SLA** (1,544), MLA60 (1,214) |
| 3100XL | LLA40 | **MLA60** (4,119), **SLA** (1,703) |
| GT2100 | TLA | **SLA** (2,570) |
| QT-350II | SLA | SLA (3,343), **MLA60** (1,908) |
| 400LLT | LLA60 | **LLA** (2,088) |

**Kết luận:** `MachineType` là nhóm **năng lực chính**, không phải giới hạn cứng. Máy lớn hơn thường gia công được cả chi tiết của nhóm nhỏ hơn. Trong scheduling cần model "capable of" list, không phải "exactly this type".

---

## 6. Epicor ERP — Database Manufacturing

> Shopfloor Manager ban đầu được phát triển như hệ thống MES nội bộ mở rộng của Epicor ERP, nhận dữ liệu Part/Job/OP qua Excel export từ Epicor. Section này ghi lại schema Epicor liên quan và cách dữ liệu được import.

### 6.1 Hierarchy tổng thể Epicor Manufacturing

```
Part (PartNum, TypeCode, UOM...)
  └── PartRev (PartNum + RevisionNum + AltMethod)
        ├── PartOpr  ← Routing template — operations
        │    PartNum, RevisionNum, OprSeq, OpCode,
        │    ResourceGrpID, ProdStandard, StdFormat,
        │    AddlSetupHours, QtyPer, AltMethod
        │
        └── PartMtl  ← BOM materials
             PartNum, RevisionNum, RelatedOperation (→ OprSeq)
             AltMethod

JobHead (JobNum, PartNum, RevisionNum, ReqQty, DueDate, OrderNum, PONum...)
  └── JobAsmbl  ← Sub-assemblies (AssemblySeq; 0 = top level)
        └── JobOper  ← Job-level operations (copy từ PartOpr khi tạo job)
              JobNum, AssemblySeq, OprSeq,
              OpCode, ResourceGrpID,
              ProdStandard, StdFormat,
              SetupHours, RunQty,
              ActProdHours (actual — ghi ngược từ labor)
              │
              ├── JobOpDtl  ← Resource detail (từng máy cụ thể)
              │    ResourceGrpID, ResourceID
              │
              └── JobMtl   ← Materials per operation
```

**Điểm quan trọng:** Khi Epicor tạo Job từ một PartRev, nó **copy** toàn bộ PartOpr thành JobOper — đây là snapshot của routing tại thời điểm tạo job (giống pattern `RoutingRevId` snapshot trong hệ thống mới).

### 6.2 Bảng `PartOpr` — Routing template (tương đương `PartOp`)

Truy vấn BAQ từ Epicor (nguồn: MachineMetrics integration doc):

```sql
SELECT [PartOpr].[Company],
       [PartOpr].[PartNum],        -- Part number
       [PartOpr].[RevisionNum],    -- Revision: A, B, C...
       [PartOpr].[OprSeq],         -- Số thứ tự: 10, 20, 30...
       [PartOpr].[OpStdID],        -- Standard operation ID
       [PartOpr].[OpCode],         -- Loại công đoạn: TURN, MILL, GRIND...
       [PartOpr].[ProdStandard],   -- Thời gian sản xuất (xem StdFormat)
       [PartOpr].[StdFormat],      -- Đơn vị: HR/HP/QH/MN
       [PartOpr].[AddlSetupHours], -- Thời gian setup (giờ)
       [PartOpr].[ResourceGrpID],  -- Nhóm máy: LATHE-MED, MILL-CNC...
       [PartOpr].[Machines],       -- Số máy cần
       [PartOpr].[QtyPer],         -- Số lượng mỗi lần
       [PartOpr].[OpsPerPart],     -- Số lần thực hiện trên 1 chi tiết
       [PartOpr].[OpDesc],         -- Mô tả công đoạn
       [PartOpr].[RunQty],
       [PartOpr].[AltMethod]       -- Phương pháp gia công thay thế
FROM Erp.PartOpr
```

**Join conditions:**
- `Part → PartRev`: `Company, PartNum`
- `PartRev → PartOpr`: `Company, PartNum, RevisionNum, AltMethod`
- `PartOpr → PartMtl`: `Company, PartNum, RevisionNum, AltMethod, OprSeq` (join RelatedOperation)

### 6.3 Bảng `JobHead` + `JobOper`

**JobHead** — header của lệnh sản xuất:

| Field | Kiểu | Tương đương Vinam |
|---|---|---|
| `JobNum` | string | `Job.JobNumber` |
| `PartNum` | string | `Job.PartRev.Part.PartNumber` |
| `RevisionNum` | string | `Job.PartRev.RevCode` |
| `ReqQty` | decimal | `Job.RunQty` |
| `DueDate` | date | `Job.ShipBy` |
| `OrderNum` / `OrderLine` | string | `PoLine.PoNumber` |
| `PONum` | string | (user stripped khi import) |
| `JobComplete` | bool | `Job.IsComplete` |
| `AssemblySeq` | int | (0 = top level, không model trong Vinam) |

**JobOper** — operations của job (copy từ PartOpr):

| Field | Kiểu | Tương đương Vinam |
|---|---|---|
| `JobNum` | string | FK → Job |
| `AssemblySeq` | int | 0 = top assembly |
| `OprSeq` | int | `PartOp.OpNumber` (int, Epicor dùng 10/20/30) |
| `OpCode` | string | `PartOp.OpType.Code` |
| `ResourceGrpID` | string | `PartOp.MachineType` (thiếu trong import hiện tại) |
| `ProdStandard` | decimal | `PartOp.ProdTime` — **phải normalize với StdFormat** |
| `StdFormat` | string | (thiếu trong import hiện tại) |
| `SetupHours` | decimal | `PartOp.SetupTime` |
| `RunQty` | decimal | (context của op) |
| `ActProdHours` | decimal | Giờ thực tế (từ labor entry — writeback) |
| `OpComplete` | bool | `PartOp.IsComplete` |

**Join conditions JobHead → JobOper:**
```
JobHead.Company = JobOper.Company
JobHead.JobNum  = JobOper.JobNum
```

**Join JobOper → JobAsmbl:**
```
+ JobOper.AssemblySeq = JobAsmbl.AssemblySeq
```

### 6.4 `ProdStandard` + `StdFormat` — Vấn đề quan trọng nhất

Epicor lưu thời gian sản xuất theo nhiều format. **Phải normalize** trước khi lưu vào `PartOp.ProdTime` (giờ/chiếc):

| `StdFormat` | Nghĩa | Công thức → giờ/chiếc |
|---|---|---|
| `HP` | Hours Per part (giờ/chiếc) | `ProdStandard` — dùng trực tiếp |
| `HR` | Hours for the Run (tổng giờ cho RunQty) | `ProdStandard / ReqQty` |
| `QH` | Quantity per Hour (chiếc/giờ) | `1.0 / ProdStandard` |
| `MN` | Minutes per part (phút/chiếc) | `ProdStandard / 60.0` |

**Rủi ro:** Nếu Excel export không bao gồm cột `StdFormat`, dữ liệu `ProdTime` bị sai cho các OP có format `HR` hoặc `QH`. Ước tính khoảng 30-40% OPs có thể bị sai format.

**Cần làm:** Khi export Excel từ Epicor, **bắt buộc** thêm cột `StdFormat` bên cạnh `ProdStandard`.

### 6.5 `AssemblySeq` — Độ phức tạp của sub-assembly

Epicor có khái niệm multi-level assembly (BOM nhiều cấp):
- `AssemblySeq = 0` → Top-level part (phần lớn chi tiết đơn của Vinam)
- `AssemblySeq > 0` → Sub-assembly (ví dụ: shaft + sleeve gia công riêng rồi lắp)

Hệ thống Vinam chỉ model single-level routing (không có sub-assembly). Nếu job có `AssemblySeq > 0` → chỉ import assembly `AssemblySeq = 0`, bỏ qua các sub-assembly.

**Lưu ý:** Phần lớn chi tiết CNC đơn giản (shaft, ring, sleeve...) chỉ có `AssemblySeq = 0` — không ảnh hưởng.

### 6.6 `AltMethod` — Routing thay thế

Epicor cho phép mỗi PartRev có nhiều routing thay thế (`AltMethod`):
- `AltMethod = ""` (empty string) → **Standard method** — routing chính
- `AltMethod = "ALT1"` → Routing thay thế (ví dụ: dùng khi máy bận)

Tương đương hệ thống mới: `Routing.Name` (Standard, Rework...).

Khi import, chỉ import `AltMethod = ""` (standard routing). Có thể mở rộng để import alt methods vào các `Routing` khác nhau trong Phase 5.

### 6.7 Field mapping tổng hợp — Epicor ↔ Vinam legacy ↔ Hệ thống mới

**Part/Revision:**

| Epicor | Legacy Vinam | Hệ thống mới | Trạng thái |
|---|---|---|---|
| `Part.PartNum` | `part.PartNumber` | `Part.PartNumber` | ✅ |
| `PartRev.RevisionNum` | `part.Revision` | `PartRev.RevCode` | ✅ |
| `PartRev.AltMethod` | `part.RoutingRevision` | `Routing.Name` | ⚠️ partial |
| `PartRev.Approved` | `part.Active` | `PartRev.IsReleased` | ✅ |

**Job:**

| Epicor | Legacy Vinam | Hệ thống mới | Trạng thái |
|---|---|---|---|
| `JobHead.JobNum` | `job.JobNumber` | `Job.JobNumber` | ✅ |
| `JobHead.PartNum + RevisionNum` | `job.PartID` (combined) | `Job.PartRevId` (snapshot) | ✅ improved |
| `JobHead.ReqQty` | `job.RunQty` | `Job.RunQty` | ✅ |
| `JobHead.DueDate` | `job.ShipBy` | `Job.ShipBy` | ✅ |
| `JobHead.OrderNum` | `job.POLineID` | `Job.PoLineId` | ⚠️ stripped |
| routing snapshot | ❌ không có | `Job.RoutingRevId` | ✅ improved |

**Operations:**

| Epicor | Legacy Vinam | Hệ thống mới | Trạng thái |
|---|---|---|---|
| `JobOper.OprSeq` | `partop.OPNumber` | `PartOp.OpNumber` + `OpNumberSort` | ✅ |
| `JobOper.OpCode` | `partop.OPType` | `PartOp.OpType.Code` | ✅ |
| `JobOper.ResourceGrpID` | `partop.OPType` (same field!) | `PartOp.MachineType` | ❌ **missing** |
| `JobOper.SetupHours` | `partop.SetupTime` | `PartOp.SetupTime` | ✅ |
| `JobOper.ProdStandard` (+ StdFormat) | `partop.ProdTime` | `PartOp.ProdTime` | ⚠️ no normalize |
| `JobOper.ForJobOnly` | `partop.ForOnlyJob` | `PartOp.ForJobOnly` | ✅ |
| `JobOper.ActProdHours` | không có | không có (Phase 5) | ⏳ |

**Resource/Machine:**

| Epicor | Legacy Vinam | Hệ thống mới | Trạng thái |
|---|---|---|---|
| `Plant.PlantNum` | `factory.FactoryID` | `Machine.FactoryId` | ⏳ Phase 5 |
| `ResourceGroup.ResourceGrpID` | `machinegroup.GroupCode` | `OpType.Code` (unified) | ⚠️ not explicit |
| `Resource.ResourceID` | `machine.MachineCode` | `Machine.Code` | ✅ |
| `Resource.ResourceGrpID` | `machine.MachineType` | `Machine.MachineType` | ✅ |
| `Resource.SerialNum` | `machine.MachineSerial` | `Machine.SerialNumber` | ⏳ |

### 6.8 Import flow hiện tại (Excel từ Epicor)

```
Epicor ERP
  → BAQ (Business Activity Query) export to Excel
  → File Excel: 2 sheets (Jobs + Operations)
  → Manual import vào Shopfloor Manager DB
```

**Cấu trúc Excel đang dùng:**

```
Sheet "Jobs":
  JobNumber | PartNumber | Revision | RunQty | ShipBy

Sheet "Operations" (JobOper JOIN JobHead):
  JobNumber | OprSeq | OpCode | SetupHours | ProdStandard | OpDesc
```

**Vấn đề với cấu trúc hiện tại:**
1. **Thiếu `StdFormat`** → ProdTime có thể sai cho ~30% OPs
2. **Thiếu `ResourceGrpID`** → mất thông tin nhóm máy (mất 1 bước cho scheduling)
3. **Không có routing snapshot** → nếu routing thay đổi sau khi import, các job cũ bị ảnh hưởng (đã fix trong hệ thống mới bằng `RoutingRevId`)
4. **Import thủ công** → error-prone, không có validation

**Cấu trúc Excel đề xuất (cải thiện):**

```
Sheet "Jobs":
  JobNumber | PartNumber | Revision | AltMethod | RunQty | ShipBy | OrderNum

Sheet "Operations":
  JobNumber | AssemblySeq | OprSeq | OpCode | ResourceGrpID |
  SetupHours | ProdStandard | StdFormat | OpDesc | ForJobOnly
```

### 6.9 Gaps trong import — Cần xử lý

| Gap | Mức độ | Xử lý đề xuất |
|---|---|---|
| `StdFormat` thiếu | **Cao** — sai data | Thêm cột vào BAQ export, thêm normalize logic khi import |
| `ResourceGrpID` thiếu | **Trung bình** — mất thông tin scheduling | Thêm vào Excel + thêm field `PartOp.ResourceGroupId` |
| `AssemblySeq` > 0 | **Thấp** — edge case | Filter `WHERE AssemblySeq = 0` trong BAQ |
| `AltMethod` != "" | **Thấp** — routing phụ | Filter `WHERE AltMethod = ''` trong BAQ |
| Import thủ công | **Trung bình** — error-prone | Xem Phase 5 REST API |

### 6.10 Phase 5 — Epicor REST API Integration

Epicor Kinetic có REST API built-in (không cần thêm license):

```
# Part & Routing template
GET /api/v2/erp/Parts/{company}/{partNum}
GET /api/v2/erp/PartRevs/{company}/{partNum}/{revisionNum}
GET /api/v2/erp/PartOprs/{company}/{partNum}/{revisionNum}/{oprSeq}

# Job data
GET /api/v2/erp/Jobs/{company}/{jobNum}
GET /api/v2/erp/JobOpers/{company}/{jobNum}/{assemblySeq}/{oprSeq}

# Labor writeback (ghi giờ thực tế ngược về Epicor)
POST /api/v2/erp/LaborHed       ← tạo labor transaction
POST /api/v2/erp/LaborDtl       ← detail: JobNum, OprSeq, LaborHrs, SetupHours
```

**Business Objects (BO) chính:**
- `Erp.BO.JobEntrySvc` — quản lý job
- `Erp.BO.LaborSvc` — nhập giờ lao động
- `Erp.BO.PartOprSvc` — routing operations

**Lợi ích khi tích hợp REST API:**
1. **Pull**: Tự động import Job mới khi Epicor phát lệnh → không cần Excel
2. **Push**: Ghi `ProductionSession.StartedAt/CompletedAt` ngược về Epicor như Labor transactions → Epicor có actual hours để so sánh với estimated
3. **Sync**: Part/Routing changes tự động cập nhật vào Shopfloor

**Bước thực hiện Phase 5:**
1. Tạo Epicor API user (read/write permissions cho JobEntry + Labor)
2. Implement `EpicorSyncService` trong Infrastructure layer
3. Webhook hoặc polling: khi Epicor tạo/update Job → trigger sync
4. Sau mỗi `ProductionSession` complete → post labor transaction về Epicor

---

## 7. Trạng thái hiện tại — Hệ thống mới

### Đã implement (Phase 4)

```csharp
public class Machine : BaseEntity
{
    public string Code { get; set; }         // ✅ MachineCode
    public string Name { get; set; }         // ✅ MachineName
    public string? MachineType { get; set; } // ✅ machinegroup.GroupCode
    public bool IsCnc { get; set; }          // ✅ CNCMachine
    public bool IsActive { get; set; }       // ✅ (không có trong legacy)
}
```

- **115 máy active** đã seed từ legacy (migration `AddMachines`, 2026-06-04)
- API: `GET /api/v1/machines?activeOnly=true`
- Desktop: Settings page ComboBox chọn máy

### Chưa implement (Phase 5)

| Field | Lý do cần | Độ ưu tiên |
|---|---|---|
| `FactoryId` | Multi-site support (2 nhà máy) | Trung bình |
| `SerialNumber` | Asset management, maintenance tracking | Thấp |
| `TravelX/Y/Z`, `MaxOd` | Validate part có gia công được trên máy không | Thấp |
| `MachineGroup` entity | Query "máy nào thuộc nhóm này?" | **Cao** — cần cho scheduling |
| `CapableGroups` (nhiều-nhiều) | Model cross-type capability (3100L có thể làm LLA + MLA) | Trung bình |

---

## 8. Thiết kế đề xuất — Phase 5

### 8.1 Entity mới: `MachineGroup`

```csharp
public class MachineGroup : BaseEntity
{
    public string Code { get; set; }          // LLA40, MIL, SLA...
    public string Name { get; set; }          // LONG LATHE THR BORE > 4.0"
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Machine> Machines { get; set; } = [];
}
```

Seed: 56 nhóm từ legacy + thêm VMM2, VML2.

### 8.2 Cập nhật `Machine`

```csharp
public class Machine : BaseEntity
{
    // Hiện có:
    public string Code { get; set; }
    public string Name { get; set; }
    public string? MachineType { get; set; }  // FK → MachineGroup.Code
    public bool IsCnc { get; set; }
    public bool IsActive { get; set; }

    // Thêm Phase 5:
    public int? FactoryId { get; set; }       // FK → Factory
    public string? SerialNumber { get; set; }
    public decimal? MaxOd { get; set; }       // mm
    public decimal? TravelX { get; set; }     // mm
    public decimal? TravelY { get; set; }
    public decimal? TravelZ { get; set; }
    public string? PowerSource { get; set; }  // AC 220/380
    public decimal? RatedCapacity { get; set; } // kW
    public DateOnly? DateOfManufacture { get; set; }

    // Navigation:
    public MachineGroup? Group { get; set; }
    public Factory? Factory { get; set; }
}
```

### 8.3 Thêm `ResourceGroupId` vào `PartOp`

```csharp
// PartOp — thêm field khi import từ Epicor:
public string? ResourceGroupId { get; set; }  // Epicor ResourceGrpID = MachineGroup.Code
```

Cho phép query: "OP 010 này nên chạy trên nhóm máy nào?" → phục vụ scheduling.

### 8.4 Query scheduling (khi có MachineGroup)

```sql
-- Máy nào available để chạy OP này?
SELECT m.* FROM machines m
WHERE m.machine_type = :op_resource_group_id
  AND m.is_cnc = true
  AND m.is_active = true
  AND m.id NOT IN (
    SELECT machine_id FROM production_sessions
    WHERE status = 'open' AND started_at IS NOT NULL
  )
```

---

## 9. Quan hệ `optype` ↔ `machinegroup` — Unified view

Trong legacy, `optype` và `machinegroup` là 2 bảng riêng nhưng cùng namespace. Hệ thống mới có thể **unify** bằng cách thêm flag vào `OpType`:

```csharp
public class OpType
{
    public int Id { get; set; }
    public string Code { get; set; }           // MLA, LLA, MIL, INS...
    public string Name { get; set; }
    public string? Description { get; set; }
    // Thêm:
    public bool IsMachineGroup { get; set; }   // true = đây là nhóm máy vật lý
    // IsMachineGroup=true: LLA, MLA, MIL, WED, INS...
    // IsMachineGroup=false: INSP (abstract), GRIND (abstract)...
}
```

Hoặc đơn giản hơn: tạo bảng `MachineGroup` riêng với seed từ `machinegroup` legacy, và `Machine.MachineType` FK đến `MachineGroup.Code`.

---

## 10. API Endpoints — Phase 5

```
✅ GET  /api/v1/machines?activeOnly=true
✅ GET  /api/v1/machines/{machineCode}/active-session
✅ GET  /api/v1/machines/{machineCode}/daily-summary

⏳ GET  /api/v1/machine-groups              -- 56 nhóm
⏳ GET  /api/v1/machine-groups/{code}/machines  -- máy thuộc nhóm này
⏳ POST /api/v1/machines
⏳ PUT  /api/v1/machines/{id}
⏳ GET  /api/v1/factories
⏳ GET  /api/v1/machines?factoryId=&groupCode=&isCnc=
⏳ GET  /api/v1/machines/{code}/oee-summary?period=week  -- OEE theo tuần/tháng
```

---

## 11. MQTT Integration (Phase 5)

Mỗi máy CNC có topic riêng:
```
factory/cnc/{machineCode}/status    ← trạng thái real-time (Running/Idle/Alarm)
factory/cnc/{machineCode}/data      ← spindle speed, feed, program number
factory/cnc/+/status                ← wildcard tất cả máy
```

`machineCode` trong MQTT = `Machine.Code` trong DB = `local.json.MachineCode` của Desktop MES.

MDC Agent (machine data collector) chạy trên PC tại mỗi máy:
- Kết nối RS-232/Ethernet vào controller FANUC/Mazak
- Parse dữ liệu → publish MQTT
- Config per-machine: `fanucconfig` table trong legacy (COM port, baud rate, IP...)

---

## 12. Edge Cases

- **Đổi MachineCode**: ảnh hưởng đến MQTT topic, `local.json` trên PC đó, `ProductionSession.MachineCode` string trong DB — không có FK cascade. Phải update thủ công cẩn thận.
- **Cross-type capability**: Máy LLA40 (3100L) thực tế gia công được cả MLA, SLA — scheduling cần model "capable groups" list thay vì chỉ primary group.
- **Máy VMM2/VML2**: Victor CNC machines thêm sau, không có machinegroup entry trong legacy — cần seed thêm khi implement MachineGroup entity.
- **Multi-factory**: Machine ở Factory 2 (Mỹ Phước) có thể khác IP network → `ApiBaseUrl` trong `local.json` có thể trỏ đến server khác hoặc cùng server nếu có VPN.
- **Process machines** (PPG, QPQ, XYL...): `IsCnc=false`, không có MQTT, không cần `local.json` — Desktop MES không deploy trên máy này.
