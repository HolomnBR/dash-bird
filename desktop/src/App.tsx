import './App.css'
import { Home } from './pages/Home'
import { Routes, Route } from 'react-router-dom'
import { Settings } from './pages/Settings.tsx'
import { DatabaseDetails } from './pages/DatabaseDetails'
import { QueryTool } from './pages/QueryTool'

function App() {
  return (
    <>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/settings" element={<Settings />} />
        <Route path="/databases/new" element={<DatabaseDetails />} />
        <Route path="/databases/:id" element={<DatabaseDetails />} />
        <Route path="/query-tool" element={<QueryTool />} />
      </Routes>
    </>
  )
}

export default App
