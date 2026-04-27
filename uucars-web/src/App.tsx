function App() {
  return (
    <div>
      <h1>UUcars</h1>
      {(() => {
        const url = import.meta.env.VITE_API_BASE_URL;
        return <p>{url}</p>;
      })()}
    </div>
  );
}

export default App;
