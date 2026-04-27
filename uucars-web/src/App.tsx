import { Card } from "@/components/ui/card";

function App() {
  return (
    <div className="flex min-h-screen items-center justify-center flex-col">
      <h1 className="text-3xl font-bold text-blue-600">UUcars</h1>

      <Card className="w-96 p-6">
        <h2 className="text-xl font-semibold mb-4">Welcome to UUcars!</h2>
        <p className="text-gray-600">
          Your one-stop solution for buying and selling cars. Explore our wide
          selection of vehicles and find your perfect match today!
        </p>
      </Card>
    </div>
  );
}

export default App;
