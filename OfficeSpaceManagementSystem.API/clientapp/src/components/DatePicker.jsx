import { DayPicker } from "react-day-picker";
import "react-day-picker/dist/style.css";

export default function DatePicker({ selectedDate, onDateSelect }) {
    const today = new Date();
    const inSevenDays = new Date();
    inSevenDays.setDate(today.getDate() + 7);

    const disabledDays = [
        {
            before: today,
            after: inSevenDays,
        },
    ];

    return (
        <div>
            <DayPicker
                mode="single"
                selected={selectedDate}
                onSelect={onDateSelect}
                disabled={disabledDays}
                modifiersClassNames={{
                    selected: 'selected',
                    disabled: 'disabled'
                }}
                styles={{
                    caption: { color: 'var(--text-color)' },
                    head: { color: 'var(--text-color)' },
                    cell: { color: 'var(--text-color)' }
                }}
            />
        </div>
    );
}
