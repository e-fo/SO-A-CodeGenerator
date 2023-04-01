namespace SOGenerator
{
    using UnityEngine;
    using UnityAtoms.BaseAtoms;

    [CreateAssetMenu(fileName = "SOPlayerProfile", menuName = "SOGenerator/SOPlayerProfile")]
    public class SOPlayerProfile : ScriptableObject, IPlayerProfile
    {
        [SerializeField]
        protected StringVariable _id;
        public string Id
        {
            get
            {
                return _id.Value;
            }

            set
            {
                _id.Value = value;
            }
        }

        [SerializeField]
        protected IntVariable _lastLogin;
        public int LastLogin
        {
            get
            {
                return _lastLogin.Value;
            }

            set
            {
                _lastLogin.Value = value;
            }
        }

        [SerializeField]
        protected IntVariable _energy;
        public int Energy
        {
            get
            {
                return _energy.Value;
            }

            set
            {
                _energy.Value = value;
            }
        }
    }
}