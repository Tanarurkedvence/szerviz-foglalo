import { useId } from 'react';
import { formatPlate, isValidPlate } from '../format';

interface Props {
  value: string;
  onChange: (value: string) => void;
}

/** A licence plate field that looks like a Hungarian plate. */
export default function PlateInput({ value, onChange }: Props) {
  const id = useId();
  const hintId = `${id}-hint`;
  const showInvalid = value.trim().length >= 6 && !isValidPlate(value);

  return (
    <div className="field">
      <label htmlFor={id}>Rendszám</label>
      <div className={`plate${showInvalid ? ' plate-invalid' : ''}`}>
        <span className="plate-strip" aria-hidden="true">
          <svg viewBox="0 0 20 20" className="plate-stars">
            {Array.from({ length: 12 }, (_, i) => {
              const angle = (i / 12) * Math.PI * 2;
              return <circle key={i} cx={10 + Math.cos(angle) * 6.5} cy={10 + Math.sin(angle) * 6.5} r="1.1" />;
            })}
          </svg>
          H
        </span>
        <input
          id={id}
          name="licensePlate"
          className="plate-input"
          value={value}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          onBlur={() => isValidPlate(value) && onChange(formatPlate(value))}
          maxLength={10}
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          aria-describedby={hintId}
          aria-invalid={showInvalid}
          placeholder="ABC-123"
        />
      </div>
      <p id={hintId} className={showInvalid ? 'hint hint-error' : 'hint'}>
        {showInvalid ? 'Ez nem magyar rendszám. Régi formátum: ABC-123, új: AA AA-123.' : 'Régi (ABC-123) vagy új (AA AA-123) formátum.'}
      </p>
    </div>
  );
}
