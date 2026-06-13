'use client'

import * as React from 'react'
import { Combobox } from '@base-ui/react/combobox'
import { va } from '@/lib/va-tokens'

export interface VAComboboxOption {
  value: string
  label: string
}

interface VAComboboxProps {
  value: string
  onChange: (value: string) => void
  options: VAComboboxOption[]
  placeholder?: string
  style?: React.CSSProperties
}

/** Select kèm gõ để tìm — dùng cho filter bar khi danh sách lựa chọn lớn (Part, OP...). */
export function VACombobox({ value, onChange, options, placeholder, style }: VAComboboxProps) {
  const selected = React.useMemo(() => options.find(o => o.value === value) ?? null, [options, value])

  return (
    <Combobox.Root<VAComboboxOption>
      items={options}
      value={selected}
      onValueChange={item => onChange(item ? item.value : 'all')}
      isItemEqualToValue={(a, b) => a?.value === b?.value}
      itemToStringLabel={item => item.label}
    >
      <Combobox.InputGroup
        className="va-combobox-group"
        style={{
          height: 32, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7,
          display: 'flex', alignItems: 'center', gap: 2, boxSizing: 'border-box',
          maxWidth: 168, ...style,
        }}>
        <Combobox.Input
          placeholder={placeholder}
          onFocus={e => e.currentTarget.select()}
          style={{
            flex: 1, minWidth: 0, height: '100%', background: 'transparent', border: 'none',
            outline: 'none', padding: '0 0 0 9px', fontSize: 12, color: va.text,
            fontFamily: 'inherit', fontWeight: 'inherit', cursor: 'text',
          }} />
        <Combobox.Trigger
          style={{
            display: 'flex', alignItems: 'center', justifyContent: 'center', width: 22,
            height: '100%', background: 'transparent', border: 'none', padding: 0,
            margin: 0, color: va.text3, cursor: 'pointer', fontSize: 10, flexShrink: 0,
          }}>
          ▾
        </Combobox.Trigger>
      </Combobox.InputGroup>
      <Combobox.Portal>
        <Combobox.Positioner sideOffset={4} align="start" style={{ zIndex: 50 }}>
          <Combobox.Popup
            style={{
              background: va.surface, border: `1px solid ${va.border}`, borderRadius: 8,
              boxShadow: va.shadowLg, padding: 4, maxHeight: 260, overflow: 'auto',
              minWidth: 160, fontFamily: va.font, fontSize: 12.5,
            }}>
            <Combobox.Empty style={{ padding: '10px 12px', fontSize: 12, color: va.text3 }}>
              Không có kết quả
            </Combobox.Empty>
            <Combobox.List>
              {(item: VAComboboxOption) => (
                <Combobox.Item
                  key={item.value}
                  value={item}
                  className="va-combobox-item"
                  style={{ padding: '7px 10px', borderRadius: 6, cursor: 'pointer', color: va.text }}>
                  {item.label}
                </Combobox.Item>
              )}
            </Combobox.List>
          </Combobox.Popup>
        </Combobox.Positioner>
      </Combobox.Portal>
    </Combobox.Root>
  )
}
