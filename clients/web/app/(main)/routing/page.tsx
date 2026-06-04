import { redirect } from 'next/navigation'

// /routing merges with /parts — Part & Routing view is at /parts
export default function RoutingPage() {
  redirect('/parts')
}
