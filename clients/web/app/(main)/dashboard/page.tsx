import { VATopbar, VAKpi, VACard } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { VASeg } from '@/components/va/seg'

export const metadata = { title: 'Dashboard — Shopfloor Manager' }

export default function DashboardPage() {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="Dashboard Sản xuất"
        breadcrumb="Tổng quan"
        right={
          <VASeg
            value="week"
            options={[
              { id: 'day',     label: 'Ngày'  },
              { id: 'week',    label: 'Tuần'  },
              { id: 'month',   label: 'Tháng' },
              { id: 'quarter', label: 'Quý'   },
            ]}
          />
        }
      />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPI row */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Máy đang chạy"    value="—"     sub="máy"      />
          <VAKpi label="SP hoàn thành"    value="—"     sub="tuần"     />
          <VAKpi label="Tỉ lệ Pass FAI"  value="—"     accent={va.ok} />
          <VAKpi label="NCR đang mở"      value="—"     sub="cần xử lý" accent={va.err} />
          <VAKpi label="Availability"     value="—"                    />
        </div>

        <VACard title="Phase 5 — Dữ liệu sẽ hiển thị tại đây" style={{ flex: 1 }}>
          <p style={{ fontSize: 13, color: va.text2, lineHeight: 1.7 }}>
            Dashboard sẽ hiển thị: trạng thái máy CNC real-time (MQTT), tiến độ Job, cảnh báo NCR &amp; Gage.<br />
            Đang chờ Phase 5: Machine Management · Planning · MQTT pipeline.
          </p>
        </VACard>
      </div>
    </div>
  )
}
