import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import SlotPicker from './SlotPicker';

const slots = ['2026-11-03T08:00:00', '2026-11-03T11:45:00', '2026-11-03T12:00:00', '2026-11-03T16:00:00'];

describe('SlotPicker', () => {
  it('splits the times into morning and afternoon', () => {
    render(<SlotPicker slots={slots} selected={null} loading={false} onSelect={() => {}} />);

    const morning = screen.getByRole('group', { name: 'Délelőtt' });
    const afternoon = screen.getByRole('group', { name: 'Délután' });
    expect(morning).toHaveTextContent('08:00');
    expect(morning).toHaveTextContent('11:45');
    expect(afternoon).toHaveTextContent('12:00');
    expect(afternoon).toHaveTextContent('16:00');
  });

  it('marks the selected time and reports clicks', async () => {
    const onSelect = vi.fn();
    render(<SlotPicker slots={slots} selected="2026-11-03T08:00:00" loading={false} onSelect={onSelect} />);

    expect(screen.getByRole('button', { name: '08:00' })).toHaveAttribute('aria-pressed', 'true');
    await userEvent.click(screen.getByRole('button', { name: '12:00' }));

    expect(onSelect).toHaveBeenCalledWith('2026-11-03T12:00:00');
  });

  it('tells the user to pick another day when nothing is free', () => {
    render(<SlotPicker slots={[]} selected={null} loading={false} onSelect={() => {}} />);

    expect(screen.getByText(/nincs szabad időpont/)).toBeInTheDocument();
  });
});
