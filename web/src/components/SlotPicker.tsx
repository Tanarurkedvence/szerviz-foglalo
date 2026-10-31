import { timeOf } from '../format';

interface Props {
  slots: string[];
  selected: string | null;
  loading: boolean;
  onSelect: (slot: string) => void;
}

/** Free start times, split into morning and afternoon. */
export default function SlotPicker({ slots, selected, loading, onSelect }: Props) {
  if (loading) {
    return <p className="muted">Szabad időpontok betöltése…</p>;
  }

  if (slots.length === 0) {
    return <p className="empty">Erre a napra nincs szabad időpont. Válassz egy másik napot.</p>;
  }

  const groups = [
    { label: 'Délelőtt', slots: slots.filter((s) => Number(timeOf(s).slice(0, 2)) < 12) },
    { label: 'Délután', slots: slots.filter((s) => Number(timeOf(s).slice(0, 2)) >= 12) },
  ].filter((g) => g.slots.length > 0);

  return (
    <div className="slot-groups">
      {groups.map((group) => (
        <div key={group.label} className="slot-group" role="group" aria-label={group.label}>
          <p className="slot-group-label">{group.label}</p>
          <div className="slots">
            {group.slots.map((slot) => (
              <button
                key={slot}
                type="button"
                className="slot"
                aria-pressed={slot === selected}
                data-testid={`slot-${timeOf(slot)}`}
                onClick={() => onSelect(slot)}
              >
                {timeOf(slot)}
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
