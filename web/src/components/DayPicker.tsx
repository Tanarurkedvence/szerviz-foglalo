import { dayLabel } from '../format';

interface Props {
  days: string[];
  selected: string;
  onSelect: (day: string) => void;
}

export default function DayPicker({ days, selected, onSelect }: Props) {
  return (
    <div className="day-strip" role="group" aria-label="Nap">
      {days.map((day) => {
        const { weekday, day: date } = dayLabel(day);
        return (
          <button
            key={day}
            type="button"
            className="day"
            aria-pressed={day === selected}
            data-testid={`day-${day}`}
            onClick={() => onSelect(day)}
          >
            <span className="day-weekday">{weekday}</span>
            <span className="day-date">{date}</span>
          </button>
        );
      })}
    </div>
  );
}
