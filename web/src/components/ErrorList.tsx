interface Props {
  messages: string[];
}

/** Shows every problem at once, read out by screen readers when it appears. */
export default function ErrorList({ messages }: Props) {
  if (messages.length === 0) return null;

  return (
    <div className="alert" role="alert">
      {messages.length === 1 ? (
        <p>{messages[0]}</p>
      ) : (
        <ul>
          {messages.map((m) => (
            <li key={m}>{m}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
