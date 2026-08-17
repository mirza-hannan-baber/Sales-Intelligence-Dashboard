export default function SuggestedQuestions({ questions, onSelectQuestion }) {
  if (!questions || questions.length === 0) return null;

  return (
    <div className="suggested-questions-container">
      <span className="suggested-title">Suggested questions:</span>
      <div className="suggested-chips">
        {questions.map((q, idx) => (
          <button
            key={idx}
            type="button"
            className="suggested-chip"
            onClick={() => onSelectQuestion(q)}
          >
            {q}
          </button>
        ))}
      </div>
    </div>
  );
}
