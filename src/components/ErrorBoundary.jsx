import { Component } from "react";

export default class ErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error) {
    return { error };
  }

  render() {
    if (this.state.error) {
      return (
        <div style={{ padding: "32px 24px" }}>
          <h2 style={{ margin: "0 0 8px 0", color: "#0f172a" }}>Unable to load this page</h2>
          <p style={{ color: "#b91c1c", marginBottom: "16px" }}>
            {this.state.error?.message || "An unexpected error occurred while rendering this view."}
          </p>
          <button
            type="button"
            className="primary-button"
            onClick={() => this.setState({ error: null })}
          >
            Try again
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
