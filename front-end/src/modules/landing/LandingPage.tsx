import { Link } from 'react-router-dom';
import {
  CalendarOutlined,
  FileTextOutlined,
  MedicineBoxOutlined,
  BellOutlined,
  LockOutlined,
  LineChartOutlined,
  UserOutlined,
  BankOutlined,
  CheckCircleOutlined,
  ArrowRightOutlined,
} from '@ant-design/icons';
import type { ComponentType } from 'react';
import { useAuthStore } from '../../store/authStore';
import { useAuth } from '../../auth/useAuth';

interface Feature {
  Icon: ComponentType<{ className?: string }>;
  title: string;
  desc: string;
}

const features: Feature[] = [
  { Icon: CalendarOutlined,    title: 'Appointment Booking', desc: 'Book consultations with doctors in seconds. Real-time slot availability.' },
  { Icon: FileTextOutlined,    title: 'Medical Records',     desc: 'Secure access to your complete medical history, anywhere, anytime.' },
  { Icon: MedicineBoxOutlined, title: 'E-Prescriptions',     desc: 'Get digital prescriptions ready for pickup at any partner pharmacy.' },
  { Icon: BellOutlined,        title: 'Smart Reminders',     desc: 'Never miss an appointment with automated SMS and email reminders.' },
  { Icon: LockOutlined,        title: 'Privacy First',       desc: 'End-to-end encryption. HIPAA-compliant data protection standards.' },
  { Icon: LineChartOutlined,   title: 'Health Analytics',    desc: 'Track your vitals, medications, and health trends over time.' },
];

const stats = [
  { number: '500+', label: 'Doctors' },
  { number: '50K+', label: 'Patients' },
  { number: '200K+', label: 'Appointments' },
  { number: '24/7', label: 'Support' },
];

export default function LandingPage() {
  const authenticated = useAuthStore(s => s.authenticated);
  const { fullName, email, roles } = useAuth();
  const isPatient = roles.includes('patient') &&
    !roles.some(r => ['admin', 'doctor', 'receptionist', 'nurse'].includes(r));

  return (
    <div className="min-h-screen bg-white">
      {/* Navbar */}
      <nav className="fixed top-0 w-full bg-white/90 backdrop-blur-md border-b border-emerald-100 z-50">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-2">
            <div className="w-9 h-9 rounded-lg bg-gradient-to-br from-emerald-600 to-emerald-800 flex items-center justify-center text-white">
              <MedicineBoxOutlined />
            </div>
            <span className="text-lg font-bold text-gray-900">Hospital HRM</span>
          </Link>

          <div className="hidden md:flex items-center gap-8 text-sm text-gray-600">
            <a href="#features" className="hover:text-emerald-700">Features</a>
            <a href="#stats" className="hover:text-emerald-700">About</a>
            <a href="#contact" className="hover:text-emerald-700">Contact</a>
          </div>

          <div className="flex items-center gap-3">
            {authenticated ? (
              <Link
                to="/dashboard"
                className="px-4 py-2 text-sm font-medium bg-emerald-700 text-white rounded hover:bg-emerald-800"
              >
                Dashboard
              </Link>
            ) : (
              <>
                <Link to="/login?as=staff" className="px-4 py-2 text-sm font-medium text-gray-700 hover:text-emerald-700">
                  Staff Login
                </Link>
                <Link to="/login?as=patient" className="px-4 py-2 text-sm font-medium bg-emerald-700 text-white rounded hover:bg-emerald-800">
                  Patient Login
                </Link>
              </>
            )}
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="pt-32 pb-20 px-6 bg-gradient-to-br from-emerald-50 via-white to-teal-50">
        <div className="max-w-6xl mx-auto text-center">
          {authenticated ? (
            <>
              <div className="inline-flex items-center gap-1.5 px-4 py-1.5 bg-emerald-100 text-emerald-800 text-xs font-semibold rounded-full mb-6">
                <CheckCircleOutlined /> Signed in as {roles[0] || 'user'}
              </div>
              <h1 className="text-4xl md:text-5xl font-bold text-gray-900 tracking-tight mb-4">
                Welcome back,{' '}
                <span className="bg-gradient-to-r from-emerald-700 to-emerald-900 bg-clip-text text-transparent">
                  {fullName?.split(' ')[0] || 'there'}
                </span>
              </h1>
              <p className="text-base text-gray-600 mb-10">{email}</p>
            </>
          ) : (
            <>
              <div className="inline-flex items-center gap-1.5 px-4 py-1.5 bg-emerald-100 text-emerald-800 text-xs font-semibold rounded-full mb-6">
                <BankOutlined /> Trusted by leading hospitals nationwide
              </div>
              <h1 className="text-4xl md:text-6xl font-bold text-gray-900 tracking-tight mb-6">
                Healthcare management,
                <br />
                <span className="bg-gradient-to-r from-emerald-700 to-emerald-900 bg-clip-text text-transparent">
                  simplified.
                </span>
              </h1>
              <p className="text-lg text-gray-600 max-w-2xl mx-auto mb-10">
                Book appointments, access medical records, manage prescriptions —
                all in one secure platform. Built for patients, doctors, and staff.
              </p>
            </>
          )}

          {authenticated && isPatient ? (
            <div className="max-w-3xl mx-auto grid md:grid-cols-3 gap-3">
              <QuickAction to="/appointments" Icon={CalendarOutlined} title="Book Appointment" sub="See available slots" />
              <QuickAction to="/medical-records" Icon={FileTextOutlined} title="Medical Records" sub="View history" />
              <QuickAction to="/pharmacy" Icon={MedicineBoxOutlined} title="Prescriptions" sub="Active meds" />
              <Link to="/dashboard" className="md:col-span-3 mt-2 inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-emerald-700 text-white rounded-lg font-medium hover:bg-emerald-800">
                Open Dashboard <ArrowRightOutlined />
              </Link>
            </div>
          ) : authenticated ? (
            <Link
              to="/dashboard"
              className="inline-block px-8 py-3 bg-emerald-700 text-white rounded-lg font-medium hover:bg-emerald-800 shadow-lg shadow-emerald-200"
            >
              Go to Dashboard
            </Link>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 max-w-2xl mx-auto">
              {/* Patient */}
              <div className="bg-white rounded-2xl p-6 border-2 border-emerald-200 hover:border-emerald-400 transition-all hover:shadow-lg">
                <UserOutlined className="text-4xl text-emerald-700 mb-3" />
                <h3 className="text-lg font-bold text-gray-900 mb-2">I'm a Patient</h3>
                <p className="text-sm text-gray-600 mb-4">
                  Book appointments, view records, manage your health.
                </p>
                <div className="flex flex-col gap-2">
                  <Link to="/register" className="w-full py-2 bg-emerald-700 text-white rounded font-medium hover:bg-emerald-800 text-sm">
                    Create Account
                  </Link>
                  <Link to="/login?as=patient" className="w-full py-2 bg-white text-emerald-700 border border-emerald-200 rounded font-medium hover:bg-emerald-50 text-sm">
                    Patient Sign In
                  </Link>
                </div>
              </div>

              {/* Staff */}
              <div className="bg-white rounded-2xl p-6 border-2 border-teal-200 hover:border-teal-400 transition-all hover:shadow-lg">
                <BankOutlined className="text-4xl text-teal-700 mb-3" />
                <h3 className="text-lg font-bold text-gray-900 mb-2">I'm Staff</h3>
                <p className="text-sm text-gray-600 mb-4">
                  Doctor or admin access with MFA required.
                </p>
                <div className="flex flex-col gap-2">
                  <Link to="/login?as=staff" className="w-full py-2 bg-teal-700 text-white rounded font-medium hover:bg-teal-800 text-sm">
                    Staff Sign In
                  </Link>
                  <div className="text-xs text-gray-500 text-center pt-1 flex items-center justify-center gap-1.5">
                    <LockOutlined /> Two-factor authentication required
                  </div>
                </div>
              </div>
            </div>
          )}

          {!authenticated && (
            <p className="mt-6 text-xs text-gray-500">
              No credit card required · Free for patients · HIPAA compliant
            </p>
          )}
        </div>
      </section>

      {/* Stats */}
      <section id="stats" className="py-16 bg-white border-y border-emerald-100">
        <div className="max-w-6xl mx-auto px-6 grid grid-cols-2 md:grid-cols-4 gap-8">
          {stats.map(s => (
            <div key={s.label} className="text-center">
              <div className="text-3xl md:text-4xl font-bold text-emerald-700">{s.number}</div>
              <div className="text-sm text-gray-500 mt-1">{s.label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Features */}
      <section id="features" className="py-20 px-6 bg-emerald-50/40">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-14">
            <h2 className="text-3xl md:text-4xl font-bold text-gray-900 mb-4">
              Everything you need, nothing you don't
            </h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Purpose-built tools for patients, doctors, nurses, and administrators.
            </p>
          </div>

          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map(f => {
              const Icon = f.Icon;
              return (
                <div
                  key={f.title}
                  className="bg-white p-6 rounded-xl border border-gray-100 hover:border-emerald-300 hover:shadow-md hover:shadow-emerald-100 transition-all"
                >
                  <Icon className="text-3xl text-emerald-700 mb-3" />
                  <h3 className="font-semibold text-gray-900 mb-2">{f.title}</h3>
                  <p className="text-sm text-gray-600 leading-relaxed">{f.desc}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 px-6 bg-gradient-to-br from-emerald-700 to-emerald-900">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-3xl md:text-4xl font-bold text-white mb-4">
            Ready to take control of your healthcare?
          </h2>
          <p className="text-emerald-100 mb-8 text-lg">
            Join thousands of patients and doctors already using Hospital HRM.
          </p>
          <Link
            to={authenticated ? '/dashboard' : '/register'}
            className="inline-flex items-center gap-2 px-8 py-3 bg-white text-emerald-700 rounded-lg font-medium hover:bg-emerald-50 shadow-xl"
          >
            {authenticated ? 'Open Dashboard' : 'Get Started Free'} <ArrowRightOutlined />
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer id="contact" className="bg-gray-900 text-gray-400 py-12 px-6">
        <div className="max-w-6xl mx-auto grid md:grid-cols-4 gap-8">
          <div>
            <div className="flex items-center gap-2 mb-3">
              <div className="w-8 h-8 rounded-lg bg-emerald-700 flex items-center justify-center text-white">
                <MedicineBoxOutlined />
              </div>
              <span className="text-white font-bold">Hospital HRM</span>
            </div>
            <p className="text-sm">Modern healthcare management for the next generation.</p>
          </div>

          <div>
            <h4 className="text-white font-semibold mb-3 text-sm">Product</h4>
            <ul className="space-y-2 text-sm">
              <li><a href="#features" className="hover:text-white">Features</a></li>
              <li><Link to="/login" className="hover:text-white">Sign In</Link></li>
              <li><Link to="/register" className="hover:text-white">Register</Link></li>
            </ul>
          </div>

          <div>
            <h4 className="text-white font-semibold mb-3 text-sm">Company</h4>
            <ul className="space-y-2 text-sm">
              <li><a href="#" className="hover:text-white">About</a></li>
              <li><a href="#" className="hover:text-white">Careers</a></li>
              <li><a href="#" className="hover:text-white">Contact</a></li>
            </ul>
          </div>

          <div>
            <h4 className="text-white font-semibold mb-3 text-sm">Legal</h4>
            <ul className="space-y-2 text-sm">
              <li><a href="#" className="hover:text-white">Privacy</a></li>
              <li><a href="#" className="hover:text-white">Terms</a></li>
              <li><a href="#" className="hover:text-white">HIPAA</a></li>
            </ul>
          </div>
        </div>

        <div className="max-w-6xl mx-auto mt-10 pt-6 border-t border-gray-800 text-center text-xs">
          © 2026 Hospital HRM. All rights reserved.
        </div>
      </footer>
    </div>
  );
}

function QuickAction({ to, Icon, title, sub }: {
  to: string;
  Icon: ComponentType<{ className?: string }>;
  title: string;
  sub: string;
}) {
  return (
    <Link to={to} className="bg-white rounded-xl p-4 border hover:border-emerald-400 hover:shadow transition text-left">
      <Icon className="text-2xl text-emerald-700 mb-1" />
      <div className="font-semibold text-sm">{title}</div>
      <div className="text-xs text-gray-500">{sub}</div>
    </Link>
  );
}
