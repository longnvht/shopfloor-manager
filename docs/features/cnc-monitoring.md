# CNC Machine Monitoring

**Route:** `/cnc`  
**Roles:** All authenticated users  
**Status:** UI scaffold complete — MQTT pipeline integration planned for Phase 5

---

## Overview

Real-time machine status dashboard powered by **MQTT** (Mosquitto broker) and pushed to the Web App via **SignalR**.

![CNC live monitoring](../screenshots/web-cnc.png)

---

## Data Flow

```
CNC Machine
    │  (FANUC FOCAS / MTConnect adapter)
    ▼
MQTT publish: factory/cnc/{machineCode}/status
    │
    ▼
Mosquitto Broker  :1883
    │
    ▼
API (ShopfloorManager.API — MQTT subscriber via MQTTnet)
    │  saves to DB + pushes to clients
    ▼
SignalR Hub  /hub/shopfloor
    │
    ▼
Web App  (real-time gauge update)
Desktop MES  (machine card on Dashboard)
```

## MQTT Topic Schema

| Topic | Payload | Description |
|---|---|---|
| `factory/cnc/{code}/status` | `{ "spindle": 85.2, "feed": 100, "alarm": false, "program": "O0021" }` | Machine status (polled every 5 s) |
| `factory/cnc/{code}/alarm` | `{ "code": "ALM-401", "message": "Servo alarm" }` | Alarm event |
| `factory/cnc/{code}/program` | `{ "name": "O0021", "line": 142 }` | Running program |

## Machine Status Fields

| Field | Description |
|---|---|
| Spindle load % | Current spindle motor load |
| Feed override % | Active feed rate override |
| In cycle | Whether a program is actively running |
| Alarm | Any active alarms |
| Running program | Current NC program name |
| Uptime today | Time with spindle running |
| Availability | `Uptime / ShiftLength × 100` |

## Supported CNC Protocols

| Protocol | Notes |
|---|---|
| FANUC FOCAS | Direct FOCAS2 API via adapter (MDC-style) |
| MTConnect | Standard XML stream → MQTT bridge |
| OPC-UA | Planned |

---

## Planned Features (Phase 5)

- Live gauge cards per machine (spindle load, feed override, alarm indicator)
- Historical availability chart (day / week)
- Alarm log with timestamps
- Dashboard integration — machine status on the Web App Dashboard KPI strip

---

## Roadmap

This view currently shows placeholder mock data. Full MQTT pipeline implementation is scheduled as part of **Phase 5 — Advanced**.
